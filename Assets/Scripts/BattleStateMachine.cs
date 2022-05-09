using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : MonoBehaviour
{
    public enum BattleState
    {
        AdvanceTime,
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
    private List<GameObject> readyUnits = new List<GameObject>();

    public float turnThreshold = 100f;
    private bool wasSimulated;

    void Start()
    {
        battleState = BattleState.AdvanceTime;

        // Find heroes and enemies in the scene with tags. Later we can replace these by reading the information from the GameManager during Awake().
        heroesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Hero"));
        enemiesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Unit"));

        combatants.AddRange(heroesInBattle);
        combatants.AddRange(enemiesInBattle);

        // Prepare the units' simulatedInitiative. 
        foreach (GameObject unit in combatants)
            {
                UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                script.simulatedInitiative = script.initiative;
            }

        // Populate the turnQueue.
        while (turnQueue.Count < 10) {
            foreach (GameObject unit in combatants)
            {
                // Add speed to the unit's simulatedInitiative to simulate the turn order.
                UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                script.simulatedInitiative += script.speed;

                // If the unit's turn comes up...
                if (script.simulatedInitiative >= turnThreshold) {
                    // ...then add the unit to readyUnits and roll over its simulatedInitiative.
                    readyUnits.Add(unit);
                    script.simulatedInitiative -= turnThreshold;
                }
            }

            // Check if any units' turns came up.
            if (readyUnits.Count > 0) {
                // Sort readyUnits by simulatedInitiative.
                readyUnits.Sort(delegate(GameObject a, GameObject b) {
                    return a.GetComponent<UnitStateMachine>().simulatedInitiative.CompareTo(b.GetComponent<UnitStateMachine>().simulatedInitiative);
                });

                // Add the units to the turnQueue by reading readyUnits back to front. Highest overflow simulatedInitiative acts first.
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
        switch (battleState) {
            case BattleState.AdvanceTime:

                // First check if anyone's ready to act.
                readyUnits.Clear();
                foreach (GameObject unit in combatants) {
                    UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                    if (script.initiative > turnThreshold) {
                        readyUnits.Add(unit);
                    }
                }
                // Sort readyUnits
                readyUnits.Sort(delegate (GameObject a, GameObject b) {
                    return a.GetComponent<UnitStateMachine>().initiative.CompareTo(b.GetComponent<UnitStateMachine>().initiative);
                });

                // If no one's ready to act...
                if (readyUnits.Count == 0) {

                    wasSimulated = true;

                    // Decide who acts next by simulating time. Start by preparing simulatedInitiative.
                    foreach (GameObject unit in combatants) {
                        UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                        script.simulatedInitiative = script.initiative;
                    }

                    // Advance simulated time until a turn comes up.
                    while (readyUnits.Count < 1) {
                        foreach (GameObject unit in combatants) {
                            // Add speed to the unit's simulatedInitiative to simulate the turn order.
                            UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                            script.simulatedInitiative += script.speed;

                            // If the unit's turn comes up...
                            if (script.simulatedInitiative >= turnThreshold) {
                                // ...then add the unit to readyUnits and roll over its simulatedInitiative.
                                readyUnits.Add(unit);
                                script.simulatedInitiative -= turnThreshold;
                            }
                        }
                    }
                }

                if (wasSimulated) {
                    // Sort readyUnits by simulatedInitiative.
                    readyUnits.Sort(delegate (GameObject a, GameObject b) {
                        return a.GetComponent<UnitStateMachine>().simulatedInitiative.CompareTo(b.GetComponent<UnitStateMachine>().simulatedInitiative);
                    });
                    readyUnits.Reverse();
                }
                
                UnitStateMachine nextUnit = readyUnits[0].GetComponent<UnitStateMachine>();

                // Advance time if we need to.
                if (wasSimulated) {
                    // Calculate how many ticks it took for the next turn to occur.
                    double initiativeDifference = turnThreshold - nextUnit.initiative;
                    double ticks = initiativeDifference / nextUnit.speed;

                    // Apply those ticks to every unit.
                    foreach (GameObject unit in combatants) {
                        UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                        script.initiative += script.speed * ticks;
                    }
                }

                // Tell the unit to act.
                nextUnit.turnState = UnitStateMachine.TurnState.Choosing;

                // Wait for it to finish.
                wasSimulated = false;
                battleState = BattleState.Idle;

                break;

            case BattleState.Idle:

                break;

            case BattleState.VictoryCheck:

                // Temporary. Victory conditions should be checked if something dies.
                battleState = BattleState.AdvanceTime;

                break;

            case BattleState.Win:
                break;

            case BattleState.Lose:
                break;
        }
    }
}
