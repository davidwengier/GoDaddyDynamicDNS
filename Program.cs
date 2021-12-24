using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

var rootCommand = new RootCommand
{
    new Option<string>("--domain",    "The domain you want to use for dynamic DNS, eg 'example.com'")     { IsRequired = true },
    new Option<string>("--subdomain", "The subdomain that should point to your IP address, eg 'gateway'") { IsRequired = true },
    new Option<string>("--key",       "The API key for the GoDaddy account.")                             { IsRequired = true },
    new Option<string>("--secret",    "The API key secret for the GoDaddy account.")                      { IsRequired = true }
};

rootCommand.Description = "GoDaddy Dynamic DNS";
rootCommand.Handler = CommandHandler.Create(async (string domain, string subdomain, string key, string secret) =>
{
    var godaddyUri = $"https://api.godaddy.com/v1/domains/{domain}/records/A/{subdomain}";

    using var client = new HttpClient();
    var ip = await client.GetStringAsync("https://api.ipify.org");
    if (!IPAddress.TryParse(ip, out var address))
    {
        Console.Error.WriteLine("Couldn't get public IP address!");
        Console.Error.WriteLine(ip);
        return 1;
    }

    Console.WriteLine($"Current public IP adderss is {ip}.");

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("sso-key", $"{key}:{secret}");

    var currentStateResponse = await client.GetAsync(godaddyUri);
    if (currentStateResponse.StatusCode != HttpStatusCode.OK)
    {
        Console.Error.WriteLine("Couldn't read A record!");
        Console.Error.WriteLine(await currentStateResponse.Content.ReadAsStringAsync());
        return 1;
    }

    //                                                                                       Love this
    var currentState = (await currentStateResponse.Content.ReadFromJsonAsync<GoDaddyResponse[]>())?[0]!;
    var desiredState = currentState with { data = ip };

    // Value equality FTW!
    if (desiredState == currentState)
    {
        Console.WriteLine("DNS is up to date, nothing to do.");
        return 0;
    }

    Console.WriteLine("Updating DNS record...");
    var response = await client.PutAsJsonAsync(godaddyUri, new[] { desiredState });

    if (response.StatusCode == HttpStatusCode.OK)
    {
        Console.WriteLine("Updated.");
        return 0;
    }

    Console.Error.WriteLine("Update failed!");
    Console.Error.WriteLine(await response.Content.ReadAsStringAsync());

    return 1;
});
return await rootCommand.InvokeAsync(args);

record GoDaddyResponse(string data, string name, string type, int ttl);
