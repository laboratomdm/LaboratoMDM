using LaboratoMDM.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LaboratoMDM.ActiveDirectory.Service.Rsop
{
    public class RsopSimulator
    {
        public RsopResult SimulateComputerRsop(
            string computerDn,
            GpoTopology topology)
        {
            var result = new RsopResult
            {
                ComputerDn = computerDn
            };

            //OU путь
            var ouPath = BuildOuPath(computerDn);

            //Быстрый доступ DN -> OU
            var ouMap = topology.OuTopology
                .ToDictionary(o => o.DistinguishedName, StringComparer.OrdinalIgnoreCase);

            var applied = new List<GpoLinkInfo>();
            bool inheritanceBlocked = false;

            // Идём снизу вверх
            foreach (var ouDn in ouPath)
            {
                if (!ouMap.TryGetValue(ouDn, out var ou))
                    continue;

                //GPO текущего OU
                foreach (var link in ou.GpoLinks)
                {
                    if (!link.Enabled)
                        continue;

                    if (!inheritanceBlocked || link.Enforced)
                    {
                        applied.Add(link);
                    }
                }

                if (ou.BlockInheritance)
                {
                    inheritanceBlocked = true;
                }
            }

            // Финальный порядок (Enforced выше, потом LinkOrder)
            var ordered = applied
                .OrderByDescending(g => g.Enforced)
                .ThenBy(g => g.LinkOrder)
                .ToList();

            int precedence = 1;

            foreach (var gpo in ordered)
            {
                result.AppliedGpos.Add(new RsopAppliedGpo
                {
                    GpoName = gpo.Gpo.DisplayName,
                    GpoGuid = gpo.Gpo.Guid,
                    Enforced = gpo.Enforced,
                    Precedence = precedence++
                });
            }

            return result;
        }

        // ===================== helpers =====================
        private List<string> BuildOuPath(string dn)
        {
            var parts = dn.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var path = new List<string>();
            var current = new List<string>();

            foreach (var part in parts)
            {
                current.Add(part);

                if (part.StartsWith("OU=", StringComparison.OrdinalIgnoreCase))
                {
                    path.Add(string.Join(",", current));
                }
            }

            return path;
        }
    }
}
