using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using Path = System.IO.Path;
using MassivesoftCore;

namespace MassivesoftTrader
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string Name { get; init; } = "MassivesoftTrader";
        public override string Author { get; init; } = "Massivesoft";
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");


        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new Dictionary<string, SemanticVersioning.Range>{
            { "com.massivesoft.massivesoftcore", new SemanticVersioning.Range(">=1.0.0") }
        };
        public override string? Url { get; init; }
        public override bool? IsBundleMod { get; init; } /*= true ;*/
        public override string? License { get; init; } = "MIT";
        public override string ModGuid { get; init; } = "com.massivesoft.massivesofttrader";
    }

    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class LoadCustomTrader(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    AddCustomTraderHelper addCustomTraderHelper,
    MassivesoftCoreClass massivesoftCoreClass
)
    : IOnLoad
    {
        private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
        private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();


        public Task OnLoad()
        {
            // A path to the mods files we use below
            var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

            // A relative path to the trader icon to show
            var traderImagePath = Path.Combine(pathToMod, "db/QB.png");

            // The base json containing trader settings we will add to the server
            var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");

            // Create a helper class and use it to register our traders image/icon + set its stock refresh time
            imageRouter.AddRoute(traderBase.Avatar.Replace(".png", ""), traderImagePath);
            addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));

            // Add our trader to the config file, this lets it be seen by the flea market
            _ragfairConfig.Traders.TryAdd(traderBase.Id, true);

            // Add our trader (with no items yet) to the server database
            // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
            // 1: The item
            // 2: The barter scheme, cost of the item (money or barter)
            // 3: The Loyalty level, what rep level is required to buy the item from trader
            addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase);

            // Add localisation text for our trader to the database so it shows to people playing in different languages
            addCustomTraderHelper.AddTraderToLocales(traderBase, "AMVD", "AMVD.");

            // Get the assort data from JSON
            var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/assort.json");

            // Save the data we loaded above into the trader we've made
            addCustomTraderHelper.OverwriteTraderAssort(traderBase.Id, assort);

            // Set default trader id to AMVD
            massivesoftCoreClass.DefaultTrader = traderBase.Id;

            // Send back a success to the server to say our trader is good to go
            return Task.CompletedTask;
        }
    }
}
