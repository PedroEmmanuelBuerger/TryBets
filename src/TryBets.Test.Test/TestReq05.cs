namespace TryBets.Test.Test;
using System.Diagnostics;
using System.IO;

public class TestReq05
{
    [Trait("Category", "5. Desenvolva os Dockerfiles dos microsserviços")]
    [Theory(DisplayName = "Todos os dockerfiles devem gerar imagens válidas")]
    [InlineData("docker-test-matches.sh", "docker-log-matches.txt", "5502")]
    [InlineData("docker-test-users.sh", "docker-log-users.txt", "5501")]
    [InlineData("docker-test-bets.sh", "docker-log-bets.txt", "5503")]
    [InlineData("docker-test-odds.sh", "docker-log-odds.txt", "5504")]
    public static void TestDockerFiles(string ShellScript, string LogFile, string Port)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = System.Environment.CurrentDirectory.ToString().Replace("bin/Debug/net6.0","") + ShellScript,
            WorkingDirectory = System.Environment.CurrentDirectory.ToString().Replace("bin/Debug/net6.0",""),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        Process proc = new Process()
        {
            StartInfo = startInfo
        };
        proc.Start();
        proc.WaitForExit();

        string saidaConsole = "";
        
        StreamReader sr = new StreamReader("../../../"+LogFile);
        string line = sr.ReadLine();
        while (line != null)
        {        
            line = sr.ReadLine();
            saidaConsole += line;
        }
        sr.Close();
        Assert.Contains("listening on", saidaConsole);
        Assert.Contains(Port, saidaConsole);
    }
}