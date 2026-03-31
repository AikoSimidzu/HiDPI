using System.IO.Compression;

string link = $"https://github.com/AikoSimidzu/HiDPI/releases/download/V{args[0]}/HiDPI.zip";

await DownloadAndUnzipAsync(link, Environment.CurrentDirectory);

async Task DownloadAndUnzipAsync(string url, string targetDirectory)
{
    string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

    try
    {
        using HttpClient client = new HttpClient();

        Console.WriteLine("Скачивание архива...");
        byte[] data = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(tempZipPath, data);

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        Console.WriteLine("Распаковка ZIP...");
        ZipFile.ExtractToDirectory(tempZipPath, targetDirectory, overwriteFiles: true);

        Console.WriteLine($"Готово! Файлы находятся в: {targetDirectory}\nНажмите на любую клавишу, чтобы закрыть это окно...");
        Console.ReadKey();
    }
    finally
    {
        if (File.Exists(tempZipPath))
            File.Delete(tempZipPath);
    }
}