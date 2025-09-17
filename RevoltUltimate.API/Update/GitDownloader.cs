using LibGit2Sharp;
using System.IO;

namespace RevoltUltimate.API.Update
{
    public class GitDownloader
    {
        public async Task CloneRepositoryAsync(string repoUrl, string localPath, IProgress<string> progressText, IProgress<int> progressPercentage)
        {
            if (Directory.Exists(localPath))
            {
                progressText?.Report("Directory already exists. Skipping clone.");
                progressPercentage?.Report(100);
                Console.WriteLine("Directory already exists. Skipping clone.");
                return;
            }

            await Task.Run(() =>
            {
                var cloneOptions = new CloneOptions();
                cloneOptions.FetchOptions.OnTransferProgress = (progress) =>
                {
                    if (progress.TotalObjects > 0)
                    {
                        int percentage = (int)(((double)progress.ReceivedObjects / progress.TotalObjects) * 80);
                        progressText?.Report($"Downloading objects: {progress.ReceivedObjects}/{progress.TotalObjects}");
                        progressPercentage?.Report(percentage);
                    }
                    return true;
                };
                cloneOptions.OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                {
                    if (totalSteps > 0)
                    {
                        int percentage = 80 + (int)(((double)completedSteps / totalSteps) * 20);
                        progressText?.Report($"Checking out files: {completedSteps}/{totalSteps}");
                        progressPercentage?.Report(percentage);
                    }
                };

                try
                {
                    Repository.Clone(repoUrl, localPath, cloneOptions);
                    progressText?.Report("Repository cloned successfully!");
                    progressPercentage?.Report(100);
                    Console.WriteLine("Repository cloned successfully!");
                }
                catch (Exception ex)
                {
                    progressText?.Report($"Error cloning repository: {ex.Message}");
                    Console.WriteLine($"Error cloning repository: {ex.Message}");
                }
            });
        }
        public void PullLatestChanges(string localPath)
        {
            try
            {
                using var repo = new Repository(localPath);
                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };

                var signature = new Signature("Revolt", "idontknow@thing.com", DateTimeOffset.Now);

                Commands.Pull(repo, signature, options);
                Console.WriteLine("Repository updated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pulling repository: {ex.Message}");
            }
        }
    }
}