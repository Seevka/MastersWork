using MastersWork.Helpers;
using MastersWork.Interfaces;
using MastersWork.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using System.IO.Compression;

namespace MastersWork.Services
{
    public class ExternalOperationsService : IExternalOperationsService
    {
        public void RunBot(BotCreationData botData)
        {

            var serverConfiguration = ConfigurationHelper.LoadServerConfiguration();
            string currentDirectory = Directory.GetCurrentDirectory();
            string projectRoot = Path.Combine(currentDirectory, @"..\");
            projectRoot = Path.GetFullPath(projectRoot);

            string filePath = Path.Combine(projectRoot, @"ChildBot\data.json");
            string folderPath = Path.Combine(projectRoot, @"ChildBot");
            string zipPath = Path.Combine(projectRoot, $"{botData.BotName}.zip");
            string localFilePath = zipPath;

            string remoteFilePath = @$"/home/severyn/Bots/{Path.GetFileName(zipPath)}";
            string remoteUnzipDir = @$"/home/severyn/Bots/{botData.BotName}";

            EditChildBotConfig(botData.Token!, botData.QA!, filePath);

            if (File.Exists(zipPath))
            {
                try
                {
                    File.Delete(zipPath);
                    Console.WriteLine("Existing ZIP file deleted.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deleting existing ZIP file: " + ex.Message);
                    return;
                }
            }

            try
            {
                ZipFile.CreateFromDirectory(folderPath, zipPath);
                Console.WriteLine($"ZIP file created successfully: {zipPath}");
                EditChildBotConfig("*", "*", filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating ZIP file: " + ex.Message);
                return;
            }

            try
            {
                using var scpClient = new ScpClient(serverConfiguration.Host, port: 22, serverConfiguration.Username, serverConfiguration.Password);
                scpClient.Connect();
                Console.WriteLine("Connected to the server via SCP.");
                using (var fileStream = new FileStream(localFilePath, FileMode.Open))
                {
                    scpClient.Upload(fileStream, remoteFilePath);
                    Console.WriteLine("File uploaded successfully using SCP.");
                }
                scpClient.Disconnect();
                Console.WriteLine("Disconnected from the server.");

                File.Delete(localFilePath);
                Console.WriteLine("Local file deleted successfully.");

                using var sshClient = new SshClient(serverConfiguration.Host, port: 22, serverConfiguration.Username, serverConfiguration.Password);
                sshClient.Connect();
                Console.WriteLine("Connected to the server via SSH.");

                var deleteDirCommand = $"rm -rf {remoteUnzipDir}";
                var deleteDirResult = sshClient.RunCommand(deleteDirCommand);
                Console.WriteLine($"Error on deleting directory on [{botData.BotName}]" + deleteDirResult.Error);

                string createDirCommand = $"mkdir -p '{remoteUnzipDir}'";
                var createDirResult = sshClient.RunCommand(createDirCommand);
                Console.WriteLine($"Error on creating dir on [{botData.BotName}]" + createDirResult.Error);

                string unzipCommand = $"unzip '{remoteFilePath}' -d '{remoteUnzipDir}'";
                var unzipResult = sshClient.RunCommand(unzipCommand);
                Console.WriteLine($"Error on unzipping on [{botData.BotName}]" + unzipResult.Error);

                string command = $"screen -dmS {botData.BotName} bash -c 'cd {remoteUnzipDir} && dotnet run ChildBot.csproj > output.log 2>&1'";
                var runResult = sshClient.RunCommand(command);
                Console.WriteLine($"Error on running {botData.BotName}" + runResult.Error);

                sshClient.Disconnect();
                Console.WriteLine("Disconnected from the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong! " + ex.Message);
            }
        }

        public void EditChildBotConfig(string token, object qa, string filePath)
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath);
                var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonContent);
                if (jsonObject != null)
                {
                    jsonObject["Token"] = token;
                    jsonObject["QA"] = JsonConvert.SerializeObject(qa);
                    File.WriteAllText(filePath, jsonObject.ToString());
                    Console.WriteLine("JSON file updated successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to parse JSON file.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating JSON file: " + ex.Message);
                return;
            }
        }

        public void StopBot(BotCreationData botData)
        {
            var serverConfiguration = ConfigurationHelper.LoadServerConfiguration();

            try
            {
                using var sshClient = new SshClient(serverConfiguration.Host, port: 22, serverConfiguration.Username, serverConfiguration.Password);
                sshClient.Connect();
                Console.WriteLine("Connected to the server via SSH.");

                string command = $"screen -XS {botData.BotName} quit";
                var runResult = sshClient.RunCommand(command);
                Console.WriteLine($"Error on stopping {botData.BotName}" + runResult.Error);

                sshClient.Disconnect();
                Console.WriteLine("Disconnected from the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong! " + ex.Message);
            }
        }
    }
}
