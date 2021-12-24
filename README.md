A simple command line tool to update a DNS A record on GoDaddy to point to the current public IP address of the computer running the tool.

Ported to C# from https://www.instructables.com/Quick-and-Dirty-Dynamic-DNS-Using-GoDaddy/

Usage:

To update gateway.example.com to point to the current IP address:
```
dotnet run --domain example.com --subdomain gateway --key <key> --secret <secret>
```

Disclaimer: Very quick and dirty, so no guarantees etc. but feel free to log issues and I'll see if I can help.
