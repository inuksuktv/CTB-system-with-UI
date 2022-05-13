using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public enum HeroGUI
    {
        Available,
        Idle,
        Done
    }
    public HeroGUI heroGUI;

    // Lists for the battle logic.
    public List<GameObject> turnQueue = new List<GameObject>();
    public List<GameObject> heroesInBattle = new List<GameObject>();
    public List<GameObject> enemiesInBattle = new List<GameObject>();
    public List<GameObject> combatants = new List<GameObject>();
    private List<GameObject> readyUnits = new List<GameObject>();

    // List of heroes ready for input. Used for GUI.
    public List<GameObject> heroesToManage = new List<GameObject>();
    public AttackHandler heroChoice;

    // Time simulation.
    public float turnThreshold = 100f;
    private int turnQueueSize = 7;
    private bool wasSimulated;

    // GUI objects
    private GameObject activeHero;
    private GameObject activePanel;
    [SerializeField] private GameObject heroPanelPrefab;
    [SerializeField] private RectTransform battleCanvas;
    public GameObject infoBox;
    private RectTransform heroPanelRT;
    private Vector2 screenPoint;
    public List<GameObject> heroPanels = new List<GameObject>();
    public List<GameObject> portraits = new List<GameObject>();
    public bool isChoosingTarget = false;

    void Start()
    {
        // Find heroes and enemies in the scene with tags. Later we can replace these by reading the information from the GameManager during Awake().
        heroesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Hero"));
        enemiesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Unit"));

        combatants.AddRange(heroesInBattle);
        combatants.AddRange(enemiesInBattle);

        // Create and place GUI Hero panels.
        foreach (GameObject hero in heroesInBattle) {
            // For each new panel, set its parent as battleCanvas and get the RectTransform of the panel.
            GameObject newPanel = Instantiate(heroPanelPrefab);
            newPanel.name = hero.name + "Panel";
            newPanel.transform.SetParent(battleCanvas);
            heroPanelRT = newPanel.GetComponent<RectTransform>();

            // Deactivate panel and add to heroPanels list.
            newPanel.SetActive(false);
            heroPanels.Add(newPanel);

            // Calculate screen position of hero (not rectTransform).
            screenPoint = Camera.main.WorldToScreenPoint(hero.transform.position);

            // Convert screen position to Canvas space (leave camera null if Screen Space Overlay).
            RectTransformUtility.ScreenPointToLocalPointInRectangle(battleCanvas, screenPoint, null, out Vector2 canvasPoint);

            // Position the panel.
            heroPanelRT.localPosition = canvasPoint;
        }

        // Prepare the units' simulatedInitiative. 
        foreach (GameObject unit in combatants)
            {
                UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                script.simulatedInitiative = script.initiative;
            }

        // Populate the turnQueue.
        while (turnQueue.Count < turnQueueSize) {
            // Simulate time.
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

        // Get the unit portaits.
        foreach (GameObject unit in turnQueue) {
            portraits.Add(unit.GetComponent<UnitStateMachine>().portrait);
        }

        // Add them to the TurnQueue GUI.
        foreach (GameObject portrait in portraits) {
            GameObject newPortrait = Instantiate(portrait);
            newPortrait.transform.SetParent(battleCanvas.Find("TurnQueue"));
        }

        infoBox.SetActive(false);

        // Start the battle.
        battleState = BattleState.AdvanceTime;
        heroGUI = HeroGUI.Available;
    }

    void Update()
    {
        switch (battleState) {
            case BattleState.AdvanceTime:
                // Check if the list was cleared by battle being won or lost.
                if (combatants.Count > 0) {
                    // First check if anyone's ready to act.
                    readyUnits.Clear();
                    foreach (GameObject unit in combatants) {
                        UnitStateMachine script = unit.GetComponent<UnitStateMachine>();
                        if (script.initiative >= turnThreshold) {
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
                }
                
                battleState = BattleState.Idle;

                break;

            case BattleState.Idle:

                break;

            case BattleState.VictoryCheck:

                if (heroesInBattle.Count < 1) {
                    battleState = BattleState.Lose;
                }
                else if (enemiesInBattle.Count < 1) {
                    battleState = BattleState.Win;
                }
                else {
                    // Refresh the GUI.
                    ClearActivePanel();
                    isChoosingTarget = false;
                }

                break;

            case BattleState.Win:

                Debug.Log("You won the battle.");
                foreach (GameObject hero in heroesInBattle) {
                    UnitStateMachine script = hero.GetComponent<UnitStateMachine>();
                    if (script.turnState == UnitStateMachine.TurnState.Acting) {
                        script.StopAllCoroutines();
                    }
                    script.turnState = UnitStateMachine.TurnState.Idle;
                }
                combatants.Clear();
                battleState = BattleState.Idle;

                break;

            case BattleState.Lose:

                Debug.Log("You lost the battle.");
                foreach (GameObject enemy in enemiesInBattle) {
                    UnitStateMachine script = enemy.GetComponent<UnitStateMachine>();
                    if (script.turnState == UnitStateMachine.TurnState.Acting) {
                        script.StopAllCoroutines();
                    }
                    script.turnState = UnitStateMachine.TurnState.Idle;
                }
                battleState = BattleState.Idle;

                break;
        }

        switch (heroGUI) {
            case (HeroGUI.Available):

                if (heroesToManage.Count > 0)
                    {
                    activeHero = heroesToManage[0];

                    // Get the hero's input panel.
                    activePanel = battleCanvas.transform.Find(activeHero.name + "Panel").gameObject;
                    activePanel.SetActive(true);

                    // Add a listener to each button which will record some attack information and change the GUI.
                    List<Button> buttons = new List<Button>(activePanel.GetComponentsInChildren<Button>());

                    int index = 0;
                    foreach (Button button in buttons)
                        {
                        RectTransform buttonRT = button.GetComponent<RectTransform>();
                        Attack attack = activeHero.GetComponent<UnitStateMachine>().attackList[index];
                        button.onClick.AddListener(() => AttackInput(activeHero, buttonRT, attack));
                        index++;
                        }

                    // Wait for the player's input.
                    heroGUI = HeroGUI.Idle;
                }

                break;

            case (HeroGUI.Idle):

                break;

            case (HeroGUI.Done):

                activeHero.GetComponent<UnitStateMachine>().CollectAttack(heroChoice);

                heroesToManage.Remove(activeHero);

                ClearActivePanel();

                heroGUI = HeroGUI.Available;

                break;
        }
    }

    public void ClearActivePanel()
    {
        // Set all the buttons opaque.
        foreach (RectTransform child in activePanel.transform) {
            Image buttonImage = child.gameObject.GetComponent<Image>();
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
            Text buttonText = child.gameObject.GetComponentInChildren<Text>();
            buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, 1f);
        }

        // Hide the panels and infobox.
        foreach (GameObject panel in heroPanels) {
            panel.SetActive(false);
        }
        infoBox.SetActive(false);
    }

    private void AttackInput(GameObject unit, Transform button, Attack attack)
    {
        heroChoice = new AttackHandler {
            // Fill what fields we can for heroChoice. Get the target on next player input.
            attackerName = unit.name,
            description = attack.description,
            chosenAttack = attack,
            attacker = unit
        };

        // Send the description to the infoBox.
        infoBox.SetActive(true);
        Text infoBoxText = infoBox.transform.Find("Text").gameObject.GetComponent<Text>();
        infoBoxText.text = heroChoice.description;

        // Set all buttons transparent.
        foreach (RectTransform child in activePanel.transform) {
            Image buttonImage = child.gameObject.GetComponent<Image>();
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 0.5f);
            Text buttonText = child.gameObject.GetComponentInChildren<Text>();
            buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, 0.5f);
        }

        // Set the button that was clicked opaque again.
        Image image = button.GetComponent<Image>();
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        Text text = button.GetComponentInChildren<Text>();
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);

        // Turn on the ClickHandler for TargetInput.
        isChoosingTarget = true;
    }

    public void TargetInput(GameObject unit)
    {
        heroChoice.target = unit;
        isChoosingTarget = false;
        ClearActivePanel();

        heroGUI = HeroGUI.Done;
    }
}
