using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : MonoBehaviour
{
    public enum BattleState
    {
        TakeCommand,
        Idle,
        VictoryCheck,
        Win,
        Lose
    }
    public BattleState battleState;

    public List<GameObject> turnQueue = new List<GameObject>();
    public List<GameObject> heroesInBattle = new List<GameObject>();
    public List<GameObject> enemiesInBattle = new List<GameObject>();
    public List<GameObject> combatants = new List<GameObject>();
    public List<GameObject> readyUnits = new List<GameObject>();

    void Start()
    {
        battleState = BattleState.TakeCommand;

        // Find heroes and enemies in the scene with tags. Later we can replace these by reading the information from the GameManager during Awake().
        heroesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Hero"));
        enemiesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Unit"));

        combatants.AddRange(heroesInBattle);
        combatants.AddRange(enemiesInBattle);

        // Populate the turnQueue.
        while (turnQueue.Count < 10) {
            foreach (GameObject unit in combatants)
            {
                // Take the unit's current initiative and add speed to get simulatedInitiative.
                BaseUnit script = unit.GetComponent<BaseUnit>();
                script.simulatedInitiative = script.initiative;
                script.simulatedInitiative += script.speed;

                // If the unit's simulatedInitiative passes the threshold, add it to readyUnits and subtract the threshold value.
                if (script.simulatedInitiative >= 100) {
                    script.simulatedInitiative -= 100;
                    readyUnits.Add(unit);
                }
            }

            if (readyUnits.Count > 0) {

                // Sort readyUnits, putting the unit with highest overflow simulatedInitiative first.
                readyUnits.Sort(delegate(GameObject a, GameObject b) {
                    return a.GetComponent<BaseUnit>().simulatedInitiative.CompareTo(b.GetComponent<BaseUnit>().simulatedInitiative);});
                readyUnits.Reverse();

                // Pop the units off readyUnits and add them to the turnQueue.
                while (readyUnits.Count > 0) {
                    turnQueue.Add(readyUnits[0]);
                    readyUnits.RemoveAt(0);
                }
            }
        }
    }

    void Update()
    {

    }
}
