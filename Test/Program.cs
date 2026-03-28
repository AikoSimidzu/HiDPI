using Test.Core;

Console.WriteLine("Запуск контроллера...");
var controller = new ZapretController();

controller.OnLogReceived += log =>
{    
    Console.ForegroundColor = log.Contains("[ОШИБКА]") ? ConsoleColor.Red : ConsoleColor.Gray;
    Console.WriteLine(log);
    Console.ResetColor();
};

controller.OnProcessExited += () =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[СИСТЕМА] Процесс winws завершился.");
    Console.ResetColor();
};

controller.OnProcessRestarted += () =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[СИСТЕМА] Обнаружено изменение config.txt! Перезапуск движка...\n\tВремя события: {DateTime.Now}");
    Console.ResetColor();
};

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[СИСТЕМА] Получен сигнал завершения. Останавливаем движок...");
    controller.Stop();
    Environment.Exit(0);
};

Console.WriteLine("Запускаем обход для Discord и YouTube...");
controller.Start();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Служба работает. Нажми Ctrl+C для безопасного выхода.");
Console.ResetColor();

Thread.Sleep(Timeout.Infinite);