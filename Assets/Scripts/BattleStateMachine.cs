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
    private List<UnitInitiatives> readyUnits = new List<UnitInitiatives>();

    // List of heroes ready for input. Used for GUI.
    public List<GameObject> heroesToManage = new List<GameObject>();
    public AttackHandler heroChoice;

    // Time simulation.
    public float turnThreshold = 100f;
    private int turnQueueSize = 7;
    public List<UnitInitiatives> unitInitiatives = new List<UnitInitiatives>();

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

        PrepareInitiative();
        readyUnits.Clear();

        // Populate the turnQueue.
        while (turnQueue.Count < turnQueueSize) {
            // Simulate time.
            foreach (UnitInitiatives unit in unitInitiatives)
            {
                unit.initiative += unit.speed;

                // If the unit's turn comes up...
                if (unit.initiative >= turnThreshold) {
                    // ...then add the unit to readyUnits and roll over its initiative.
                    readyUnits.Add(unit);
                    unit.initiative -= turnThreshold;
                }
            }

            // Check if any units' turns came up.
            if (readyUnits.Count > 0) {
                // Sort readyUnits by initiative.
                readyUnits.Sort(delegate(UnitInitiatives a, UnitInitiatives b) {
                    return a.initiative.CompareTo(b.initiative);
                });

                // Add the units to the turnQueue by reading readyUnits back to front. Highest overflow initiative acts first.
                for (int i = readyUnits.Count; i > 0; i--) {
                    turnQueue.Add(readyUnits[i-1].unitGO);
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

                // Check if the battle was cleared by winning or losing.
                if (combatants.Count == 0) { battleState = BattleState.Idle; }

                readyUnits.Clear();
                PrepareInitiative();

                // Check if anyone's ready to act.
                foreach (UnitInitiatives unit in unitInitiatives) {
                    if (unit.initiative >= 100) {
                        readyUnits.Add(unit);
                    }
                }
                
                // If no one's ready to act, advance time. Find all turns that would come up over the duration of a 'tick'.
                if (readyUnits.Count == 0) {
                    // Simulate time until a turn comes up.
                    while (readyUnits.Count < 1) {
                        foreach (UnitInitiatives unit in unitInitiatives) {
                            unit.initiative += unit.speed;

                            // Check if a unit's turn came up and add it to readyUnits.
                            if (unit.initiative >= turnThreshold) {
                                readyUnits.Add(unit);
                            }
                        }
                    }
                }

                // Sort readyUnits by initiative.
                readyUnits.Sort(delegate (UnitInitiatives a, UnitInitiatives b) {
                    return a.initiative.CompareTo(b.initiative);
                });

                // The actor is the last member of readyUnits (highest initiative). Get its script.
                UnitStateMachine actor = readyUnits[readyUnits.Count - 1].unitGO.GetComponent<UnitStateMachine>();

                // Tell the actor to act and wait until you hear back.
                actor.turnState = UnitStateMachine.TurnState.Choosing;     
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
        if (activePanel != null) {
            foreach (RectTransform child in activePanel.transform) {
                Image buttonImage = child.gameObject.GetComponent<Image>();
                buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
                Text buttonText = child.gameObject.GetComponentInChildren<Text>();
                buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, 1f);
            }
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

    private void PrepareInitiative()
    {
        unitInitiatives.Clear();

        // Read a combatant's fields into a new member of unitInitiatives. 
        foreach (GameObject combatant in combatants) {
            UnitInitiatives currentUnit = new UnitInitiatives();
            UnitStateMachine script = combatant.GetComponent<UnitStateMachine>();

            currentUnit.unitGO = combatant;
            currentUnit.initiative = script.initiative;
            currentUnit.speed = script.speed;
            unitInitiatives.Add(currentUnit);
        }
    }
}
