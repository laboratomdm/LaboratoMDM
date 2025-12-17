namespace LaboratoMDM.PolicyEngine
{
    public interface IArchiver
    {
        string Archive(string sourcePath, string targetPath);
    }
}
