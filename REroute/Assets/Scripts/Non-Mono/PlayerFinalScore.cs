using System;

[Serializable]
public struct PlayerFinalScore
    {
        public string playerName;
        public int finishPosition;
        public float raceTime;
        public int actionScore;
        public int timePenalty;
        public int finalScore;

        public PlayerFinalScore(string name, int position, float time, int actScore, int penalty, int final)
        {
            playerName = name;
            finishPosition = position;
            raceTime = time;
            actionScore = actScore;
            timePenalty = penalty;
            finalScore = final;
        }
    }