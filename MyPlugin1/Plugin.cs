using Terraria;
using TerrariaApi.Server;

namespace MyPlugin1;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Name => "MyPlugin1";
    public override string Author => "None";
    public override string Description => "None";
    public override Version Version => new Version(1, 0);
    public Plugin(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
        
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {

        }
    }
}
