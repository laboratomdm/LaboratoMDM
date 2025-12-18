using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public sealed class SupportedOnResolver(ISupportedOnCatalog catalog) : ISupportedOnResolver
    {
        public PolicyApplicabilityResult Resolve(
            string supportedOnRef,
            PolicyEvaluationContext context)
        {
            var def = catalog.Find(supportedOnRef);
            if (def == null)
            {
                return new PolicyApplicabilityResult
                {
                    Status = PolicyApplicabilityStatus.PolicyNotFound,
                    Reason = $"SupportedOn '{supportedOnRef}' not found"
                };
            }

            return Evaluate(def.RootExpression, context);
        }

        private PolicyApplicabilityResult Evaluate(
            SupportedOnExpression expr,
            PolicyEvaluationContext context)
        {
            return expr switch
            {
                SupportedOnOr or => EvalOr(or, context),
                SupportedOnAnd and => EvalAnd(and, context),
                SupportedOnReference r => EvalReference(r, context),
                SupportedOnRange range => EvalRange(range, context),
                _ => PolicyApplicabilityResult.Unknown("Unsupported expression")
            };
        }

        private PolicyApplicabilityResult EvalOr(
            SupportedOnOr or,
            PolicyEvaluationContext context)
        {
            var unknown = false;

            foreach (var item in or.Items)
            {
                var r = Evaluate(item, context);
                if (r.Status == PolicyApplicabilityStatus.Applicable)
                    return r;

                if (r.Status == PolicyApplicabilityStatus.Unknown)
                    unknown = true;
            }

            return unknown
                ? PolicyApplicabilityResult.Unknown("OR contains unknown expressions")
                : PolicyApplicabilityResult.NotApplicable("No OR conditions matched");
        }

        private PolicyApplicabilityResult EvalAnd(
            SupportedOnAnd and,
            PolicyEvaluationContext context)
        {
            foreach (var item in and.Items)
            {
                var r = Evaluate(item, context);
                if (r.Status != PolicyApplicabilityStatus.Applicable)
                    return r;
            }

            return PolicyApplicabilityResult.Applicable("All AND conditions matched");
        }

        private PolicyApplicabilityResult EvalReference(
            SupportedOnReference reference,
            PolicyEvaluationContext context)
        {
            // Минимальная базовая логика
            if (reference.Ref.Contains("Windows10", StringComparison.OrdinalIgnoreCase))
            {
                return context.OsVersion.Major >= 10
                    ? PolicyApplicabilityResult.Applicable("Windows 10+")
                    : PolicyApplicabilityResult.NotApplicable("Requires Windows 10+");
            }

            // reference может ссылаться на другой supportedOn
            var nested = catalog.Find(reference.Ref);
            if (nested != null)
            {
                return Evaluate(nested.RootExpression, context);
            }

            return PolicyApplicabilityResult.Unknown(
                $"Unresolved reference '{reference.Ref}'");
        }

        private static PolicyApplicabilityResult EvalRange(
            SupportedOnRange range,
            PolicyEvaluationContext context)
        {
            if (context.OsVersion == null)
            {
                return PolicyApplicabilityResult.Unknown(
                    "OS version index not available");
            }

            if (range.MinVersionIndex.HasValue &&
                context.OsVersion.Revision < range.MinVersionIndex)
            {
                return PolicyApplicabilityResult.NotApplicable(
                    $"Version index < {range.MinVersionIndex}");
            }

            if (range.MaxVersionIndex.HasValue &&
                context.OsVersion.Revision > range.MaxVersionIndex)
            {
                return PolicyApplicabilityResult.NotApplicable(
                    $"Version index > {range.MaxVersionIndex}");
            }

            return PolicyApplicabilityResult.Applicable("Version index in range");
        }
    }
}
