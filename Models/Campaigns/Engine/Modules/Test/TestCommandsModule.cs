using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Commands;
using DungeonApp.Models.Campaigns.Engine.Events;

namespace DungeonApp.Models.Campaigns.Engine.Modules.Test;

/// <summary>
/// Moduł testowy rejestrujący przykładowe komendy z rozgałęzieniami —
/// poligon doświadczalny dla systemu autouzupełniania (Brigadier-style).
/// </summary>
/// <remarks>
/// DLACZEGO osobny moduł testowy:
/// Testowe komendy (/weather, /spawn, /teleport) nie mają prawdziwej logiki wykonania —
/// ich celem jest weryfikacja poprawności silnika autouzupełniania z różnymi typami węzłów.
/// Izolacja w osobnym module pozwala dodawać go selektywnie (np. tylko w Sandboxie)
/// bez zaśmiecania produkcyjnego CampaignEngine.
/// </remarks>
public class TestCommandsModule : CampaignModuleBase
{
    public override string ModuleId => "Test.Commands";

    protected override void OnInitialize()
    {
        base.OnInitialize();
        RegisterTestCommands();
    }

    private void RegisterTestCommands()
    {
        /*
         * 1. Komenda z czasem (Testowanie TimeArgumentType)
         * /time add <<czas>>
         * /time set <pora: morning|noon|evening|night>
         */
        var timeTree = CommandTree.Literal("/time")
            .Then(CommandTree.Literal("add")
                .Then(CommandTree.Time("czas").Executes(ctx => {
                    int seconds = ctx.GetArgument<int>("czas");
                    return CommandResult.Success($"Dodano czas: {seconds} sekund.");
                })))
            .Then(CommandTree.Literal("set")
                .Then(CommandTree.Enum("pora", "morning", "noon", "evening", "night").Executes(ctx => {
                    string pora = ctx.GetArgument<string>("pora");
                    return CommandResult.Success($"Ustawiono porę na: {pora}.");
                })));

        Publish(new RegisterCommandEvent(timeTree.Root, ModuleId));

        /*
         * 2. Komenda z ograniczonymi liczbami (Testowanie IntegerArgumentType min/max)
         * /heal <<cel: string>> <<hp: 1-100>>
         */
        var healTree = CommandTree.Literal("/heal")
            .Then(CommandTree.String("cel")
                .Then(CommandTree.Number("hp", min: 1, max: 100).Executes(ctx => {
                    string cel = ctx.GetArgument<string>("cel");
                    int hp = ctx.GetArgument<int>("hp");
                    return CommandResult.Success($"Wyleczono {cel} za {hp} HP.");
                })));

        Publish(new RegisterCommandEvent(healTree.Root, ModuleId));

        /*
         * 3. Komenda mieszająca wszystko (Krótkie stringi, Enum, Number)
         * /give <<gracz: string>> <przedmiot: sword|bow|potion|gold> <<ilosc: 1-64>>
         */
        var giveTree = CommandTree.Literal("/give")
            .Then(CommandTree.String("gracz")
                .Then(CommandTree.Enum("przedmiot", "sword", "bow", "potion", "gold")
                    .Then(CommandTree.Number("ilosc", min: 1, max: 64).Executes(ctx => {
                        string gracz = ctx.GetArgument<string>("gracz");
                        string przedmiot = ctx.GetArgument<string>("przedmiot");
                        int ilosc = ctx.GetArgument<int>("ilosc");
                        return CommandResult.Success($"Gracz {gracz} otrzymał {ilosc}x {przedmiot}.");
                    }))));

        Publish(new RegisterCommandEvent(giveTree.Root, ModuleId));

        /*
         * 4. Bardzo długa i złożona ścieżka z jednostką czasu i siłą
         * /effect <<cel: string>> <efekt: poison|haste|slowness|regeneration> <<czas>> <<sila: 1-10>>
         */
        var effectTree = CommandTree.Literal("/effect")
            .Then(CommandTree.String("cel")
                .Then(CommandTree.Enum("efekt", "poison", "haste", "slowness", "regeneration")
                    .Then(CommandTree.Time("czas_trwania")
                        .Then(CommandTree.Number("sila", min: 1, max: 10).Executes(ctx => {
                            string cel = ctx.GetArgument<string>("cel");
                            string efekt = ctx.GetArgument<string>("efekt");
                            int czas = ctx.GetArgument<int>("czas_trwania");
                            int sila = ctx.GetArgument<int>("sila");
                            return CommandResult.Success($"Nałożono efekt {efekt} (siła {sila}) na {cel} na {czas} sekund.");
                        })))));

        Publish(new RegisterCommandEvent(effectTree.Root, ModuleId));

        /*
         * 5. Komenda z Greedy Stringiem (akceptuje tekst ze spacjami na końcu)
         * /say <<wiadomosc: greedy_string>>
         */
        var sayTree = CommandTree.Literal("/say")
            .Then(CommandTree.String("wiadomosc", isGreedy: true).Executes(ctx => {
                string msg = ctx.GetArgument<string>("wiadomosc");
                return CommandResult.Info(msg);
            }));

        Publish(new RegisterCommandEvent(sayTree.Root, ModuleId));
        
        /*
         * 6. Komenda z wieloma cyframi pod rząd (np. kordy X Y Z)
         * /teleport <<x: number>> <<y: number>> <<z: number>>
         */
        var teleportTree = CommandTree.Literal("/teleport")
            .Then(CommandTree.Number("x")
                .Then(CommandTree.Number("y")
                    .Then(CommandTree.Number("z").Executes(ctx => {
                        int x = ctx.GetArgument<int>("x");
                        int y = ctx.GetArgument<int>("y");
                        int z = ctx.GetArgument<int>("z");
                        return CommandResult.Success($"Teleportacja na kordy: {x}, {y}, {z}.");
                    }))));
                    
        Publish(new RegisterCommandEvent(teleportTree.Root, ModuleId));
    }
}
