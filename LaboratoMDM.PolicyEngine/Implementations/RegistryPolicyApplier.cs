using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.NodeEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class RegistryPolicyApplier(
        ILogger<RegistryPolicyApplier> logger
        ) : IPolicyApplier
    {
        public void Apply(
            PolicyApplicationPlan plan,
            PolicyExecutionContext context)
        {
            logger.LogInformation(
                "Applying policy {Policy} (Machine={Machine}, UserSid={UserSid}, Group={Group})",
                plan.PolicyName,
                context.IsMachine,
                context.UserSid,
                context.UserGroup);

            if (context.UserGroup != null)
            {
                ApplyToGroup(plan, context.UserGroup);
                return;
            }

            ApplyToTarget(plan, context.UserSid);
        }

        public bool IsApplied(
            PolicyApplicationPlan plan, 
            PolicyExecutionContext context)
        {
            logger.LogInformation(
                "Checking policy state {Policy} (Machine={Machine}, UserSid={UserSid}, Group={Group})",
                plan.PolicyName,
                context.IsMachine,
                context.UserSid,
                context.UserGroup);

            if (context.UserGroup != null)
                return IsAppliedToGroup(plan, context.UserGroup);

            return IsAppliedToTarget(plan, context.UserSid);
        }


        private void ApplyToTarget(
            PolicyApplicationPlan plan,
            string? userSid)
        {
            foreach (var op in plan.Operations)
            {
                using var root = ResolveRoot(op.Scope, userSid);

                ExecuteOperation(root, op);
            }
        }

        private void ApplyToGroup(
            PolicyApplicationPlan plan,
            string groupName)
        {
            var sids = LocalUserResolver.GetUserSidsByGroup(groupName);

            foreach (var sid in sids)
            {
                logger.LogDebug("Applying policy to group member SID {Sid}", sid);
                ApplyToTarget(plan, sid);
            }
        }

        private static RegistryKey ResolveRoot(
            PolicyScope scope,
            string? userSid)
        {
            if (scope == PolicyScope.Machine)
                return Registry.LocalMachine;

            if (!string.IsNullOrEmpty(userSid))
                return Registry.Users.OpenSubKey(userSid, writable: true)
                       ?? throw new InvalidOperationException($"HKU\\{userSid} not loaded");

            return Registry.CurrentUser;
        }

        private bool IsAppliedToGroup(
            PolicyApplicationPlan plan,
            string groupName)
        {
            var sids = LocalUserResolver.GetUserSidsByGroup(groupName);

            if (!sids.Any())
            {
                logger.LogWarning(
                    "Group {Group} has no resolved users",
                    groupName);
                return false;
            }

            foreach (var sid in sids)
            {
                logger.LogDebug(
                    "Checking policy {Policy} for group member {Sid}",
                    plan.PolicyName,
                    sid);

                if (!IsAppliedToTarget(plan, sid))
                    return false;
            }

            return true;
        }

        private bool IsAppliedToTarget(
            PolicyApplicationPlan plan,
            string? userSid)
        {
            foreach (var op in plan.Operations)
            {
                using var root = ResolveRoot(op.Scope, userSid);

                if (!IsOperationApplied(root, op))
                {
                    logger.LogDebug(
                        "Operation not applied: {Key}\\{Value}",
                        op.Key,
                        op.ValueName);
                    return false;
                }
            }

            return true;
        }

        private bool IsOperationApplied(
            RegistryKey root,
            RegistryOperation op)
        {
            try
            {
                using var key = root.OpenSubKey(op.Key);

                if (op.Delete)
                {
                    return key == null;
                }

                if (key == null)
                    return false;

                var value = key.GetValue(op.ValueName);

                if (op.Value == null)
                    return value == null;

                return value != null && value.Equals(op.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to check registry operation {Reason}",
                    op.Reason);
                return false;
            }
        }



        private void ExecuteOperation(
            RegistryKey root,
            RegistryOperation op)
        {
            try
            {
                if (op.Delete)
                {
                    root.DeleteSubKeyTree(op.Key, false);
                    logger.LogDebug("Deleted {Key}\\{Value}", op.Key, op.ValueName);
                    return;
                }

                using var key = root.CreateSubKey(op.Key);
                if (op.Value != null)
                {
                    key!.SetValue(op.ValueName, op.Value, op.ValueKind);
                    logger.LogDebug(
                        "Set {Key}\\{Value} = {Data}",
                        op.Key, op.ValueName, op.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to apply operation {Reason}",
                    op.Reason);
                throw;
            }
        }
    }
}
