using System.Globalization;

namespace LaboratoMDM.PolicyEngine.Utils
{
    public class AdmlUtils
    {
        public static string ExtractLangCode(string admlFilePath)
        {
            var directory = Path.GetDirectoryName(admlFilePath)
                ?? throw new InvalidOperationException(
                    $"Cannot determine directory for ADML file: {admlFilePath}");

            var langCode = Path.GetFileName(directory);

            if (string.IsNullOrWhiteSpace(langCode))
                throw new InvalidOperationException(
                    $"Cannot determine language code from path: {admlFilePath}");

            // Проверяем, что это валидная locale
            try
            {
                _ = CultureInfo.GetCultureInfo(langCode);
                return langCode;
            }
            catch (CultureNotFoundException)
            {
                throw new InvalidOperationException(
                    $"Directory '{langCode}' is not a valid culture name for ADML file '{admlFilePath}'");
            }
        }
    }
}
