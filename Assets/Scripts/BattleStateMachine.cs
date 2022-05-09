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

    private float turnThreshold = 100f;

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
                BaseUnit script = unit.GetComponent<BaseUnit>();
                script.simulatedInitiative = script.initiative;
            }

            foreach (GameObject unit in combatants)
            {
                // Add speed to the unit's initiative to simulate the turn order.
                BaseUnit script = unit.GetComponent<BaseUnit>();
                script.simulatedInitiative += script.speed;

                // If the unit's turn comes up...
                if (script.simulatedInitiative >= turnThreshold) {

                    // ...Then add the unit to readyUnits and roll over its initiative.
                    readyUnits.Add(unit);
                    script.simulatedInitiative -= turnThreshold;
                }
            }

            // If any units' turns came up this pass through the loop...
            if (readyUnits.Count > 0) {

                // Sort readyUnits by initiative.
                readyUnits.Sort(delegate(GameObject a, GameObject b) {
                    return a.GetComponent<BaseUnit>().simulatedInitiative.CompareTo(b.GetComponent<BaseUnit>().simulatedInitiative);
                });
                // Add the units to the turnQueue by reading readyUnits back to front. Highest overflow initiative acts first.
                for (int i = readyUnits.Count; i > 0; i--) {
                    turnQueue.Add(readyUnits[i-1]);
                }
                // Clear the list.
                readyUnits.Clear();
            }
        }
    }

    void Update()
    {

    }
}
