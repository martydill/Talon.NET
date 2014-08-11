Talon.NET
===
Talon.net is a .NET implementation of Mailgun's awesome talon email processing library (https://github.com/mailgun/talon)

Status
===
Please note that Talon.NET is ***incomplete***, and is still a work in progress!
It currently supports plaintext reply extraction and bruteforce signature extraction.

It does ***not*** yet support HTML reply extraction, machine learning signature extraction, or any other talon features.

[![Build status](https://ci.appveyor.com/api/projects/status/t185rse890gdkcam)](https://ci.appveyor.com/project/martydill/talon-net)

Installation
===
Use NuGet!
```
PM> install-package Talon.NET
```

Usage
===
Extracting a reply from a text message:
```c#
using Talon;

class Program
{
    static void Main(string[] args)
    {
        var text = @"Reply

                    -----Original Message-----

                    Quote";

        var reply = Quotations.ExtractFrom(text, "text/plain");
        reply = Quotations.ExtractFromPlain(text);
        // reply == "Reply"
    }
}
```

Extracting a signature using the brute force method:
```c#
using Talon;

class Program
{
    static void Main(string[] args)
    {
        var text = @"Wow. Awesome!
                    --
                    Bob Smith";

        var result = Bruteforce.ExtractSignature(text);
        // result.Item1 = "Wow. Awesome!"
        // result.Item2 = "--\r\nBob Smith"
    }
}

```

