namespace Olve.Trains.Scenes.GameLogic.Stations;

public record StationPool(IReadOnlyCollection<string> StationNames)
{
    public static readonly StationPool Yamanote = new([
        "Gotanda",
        "Osaki",
        "Shinagawa",
        "Takanawa Gateway",
        "Tamachi",
        "Hamamatsucho",
        "Shimbashi",
        "Yurakucho",
        "Tokyo",
        "Kanda",
        "Akihabara",
        "Okachimachi",
        "Ueno",
        "Uguisudani",
        "Nippori",
        "Nishi-Nippori",
        "Tabata",
        "Komagome",
        "Sugamo",
        "Ootsuka",
        "Ikebukuro",
        "Mejiro",
        "Takadanobaba",
        "Shin-Okubo",
        "Shinjuku",
        "Yoyogi",
        "Harajuku",
        "Shibuya",
        "Ebisu",
        "Meguro"
    ]);
}