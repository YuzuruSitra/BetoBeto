using BetoBeto.Stage;

namespace BetoBeto.Core
{
    public enum FruitKind { Strawberry, Blueberry, Orange, Melon }
    public enum GameState { Title, Playing, Paused, Won, Lost }

    /// <summary>Rules and counters have no dependency on scene objects or UI.</summary>
    public sealed class GameSession
    {
        public readonly int[] Harvested = new int[4];
        public Recipe Recipe { get; }
        public int EscapeLimit { get; }
        public int Escaped { get; private set; }
        public int TotalHarvested { get; private set; }
        public int BestChain { get; private set; }
        public int Score { get; private set; }
        public GameState State { get; set; } = GameState.Title;
        public float Elapsed { get; set; }
        public int RecipeCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 4; i++) count += System.Math.Min(Harvested[i], Recipe.For((FruitKind)i));
                return count;
            }
        }
        public GameSession(StageData data) { Recipe = data.recipe; EscapeLimit = data.escapeLimit; }
        public void RecordChain(int chain)
        {
            if (State == GameState.Playing) BestChain = System.Math.Max(BestChain, chain);
        }
        public void Harvest(FruitKind kind, int chain)
        {
            if (State != GameState.Playing) return;
            Harvested[(int)kind]++;
            TotalHarvested++;
            BestChain = System.Math.Max(BestChain, chain);
            Score += 100 * System.Math.Max(1, chain);
            if (RecipeCount >= Recipe.Total) State = GameState.Won;
        }
        public void Escape()
        {
            if (State != GameState.Playing) return;
            if (++Escaped >= EscapeLimit) State = GameState.Lost;
        }
    }
}
