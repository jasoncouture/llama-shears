using System.Text.RegularExpressions;

private var pattern = @"^(?=.{1,90}$)[a-z]{1,10}(?:\(.+\))*!?(?::).{4,}(?:#\d+)*(?<![\.\s])$";
private var msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
   return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("Invalid commit message");
Console.ResetColor();
Console.WriteLine("e.g: 'feat(scope): subject' or 'fix: subject'");
Console.WriteLine("type: lowercase alpha, max 10 chars; subject must be ≤ 90 chars total and not end with '.' or whitespace.");
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("more info: https://www.conventionalcommits.org/en/v1.0.0/");

return 1;
