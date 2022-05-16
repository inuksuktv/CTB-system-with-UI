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
    public float turnThreshold = 1000f;
    private readonly int turnQueueTargetSize = 8;
    public List<UnitInitiatives> unitInitiatives = new List<UnitInitiatives>();

    // GUI objects
    private GameObject activeHero;
    [SerializeField] private GameObject heroPanelPrefab;
    private GameObject activePanel;
    private RectTransform heroPanelRT;
    [SerializeField] private RectTransform battleCanvas;
    [SerializeField] private GameObject turnQueuePrefab;
    private RectTransform turnQueueRT;
    [SerializeField] private GameObject turnPanelPrefab;
    [SerializeField] private GameObject infoBoxPrefab;
    private GameObject infoBox;

    private Vector2 screenPoint;

    public List<GameObject> heroPanels = new List<GameObject>();
    public List<Portraits> portraits = new List<Portraits>();
    
    
    public bool isChoosingTarget = false;

    void Start()
    {
        // Find heroes and enemies in the scene with tags. Later we can replace these by reading the information from the GameManager during Awake().
        heroesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Hero"));
        enemiesInBattle.AddRange(GameObject.FindGameObjectsWithTag("Unit"));

        combatants.AddRange(heroesInBattle);
        combatants.AddRange(enemiesInBattle);

        // Create and place GUI elements.
        CreateHeroPanels();
        turnQueueRT = Instantiate(turnQueuePrefab, battleCanvas).GetComponent<RectTransform>();
        infoBox = Instantiate(infoBoxPrefab, battleCanvas);
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

                // Cache unit information.
                PrepareInitiative();

                // Simulate time and build a turnQueue.
                GenerateQueue();

                // Check if anyone's ready to act.
                foreach (UnitInitiatives unit in unitInitiatives) {
                    if (unit.initiative >= turnThreshold) {
                        readyUnits.Add(unit);
                    }
                }
                
                // The actor is the first member of the turnQueue. Get its script.
                UnitStateMachine actor = turnQueue[0].GetComponent<UnitStateMachine>();

                // Apply ticks to each unit's initiative.
                if (actor.initiative <= turnThreshold) {
                    double initiativeDifference = turnThreshold - actor.initiative;
                    double ticks = initiativeDifference / actor.speed;

                    foreach (UnitInitiatives unit in unitInitiatives) {
                        UnitStateMachine script = unit.unitGO.GetComponent<UnitStateMachine>();
                        script.initiative += script.speed * ticks;    
                    }
                }

                // Send the turnQueue to the GUI.
                SendPortraitsToGUI();

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
                    // Refresh the input GUI.
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
                        Text buttonText = button.transform.Find("Text").GetComponent<Text>();

                        Attack attack = activeHero.GetComponent<UnitStateMachine>().attackList[index];
                        buttonText.text = attack.attackName;

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

    public void TargetInput(GameObject unit)
    {
        heroChoice.target = unit;
        isChoosingTarget = false;
        ClearActivePanel();

        heroGUI = HeroGUI.Done;
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

    private void CreateHeroPanels()
    {
        foreach (GameObject hero in heroesInBattle) {
            // Create and name the heroPanel.
            GameObject newPanel = Instantiate(heroPanelPrefab, battleCanvas);
            newPanel.name = hero.name + "Panel";

            // Deactivate panel and add to heroPanels list.
            newPanel.SetActive(false);
            heroPanels.Add(newPanel);

            // Position the panel.
            screenPoint = Camera.main.WorldToScreenPoint(hero.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(battleCanvas, screenPoint, null, out Vector2 canvasPoint);
            newPanel.GetComponent<RectTransform>().localPosition = canvasPoint;
        }
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

    private void GenerateQueue()
    {
        turnQueue.Clear();
        while (turnQueue.Count < turnQueueTargetSize)
            {
            readyUnits.Clear();

            // Tick the units forward.
            foreach (UnitInitiatives unit in unitInitiatives) {
                unit.initiative += unit.speed;

                // If the unit's turn comes up, add it to the list.
                if (unit.initiative >= turnThreshold) {
                    readyUnits.Add(unit);
                    unit.initiative -= turnThreshold;
                }
            }

            // Check if any units' turns came up.
            if (readyUnits.Count > 0) {
                // Sort by initiative.
                readyUnits.Sort(delegate (UnitInitiatives a, UnitInitiatives b) {
                    return a.initiative.CompareTo(b.initiative);
                });

                // Add the units to the turnQueue by reading readyUnits back to front. Highest overflow initiative acts first.
                for (int i = readyUnits.Count; i > 0; i--) {
                    turnQueue.Add(readyUnits[i - 1].unitGO);
                }
            }
        }
    }

    private void SendPortraitsToGUI()
    {
        foreach (RectTransform child in turnQueueRT) {
            Destroy(child.gameObject);
        }
        portraits.Clear();

        foreach (GameObject unit in turnQueue)
        {
            // Fill out the fields for the new panel.
            Portraits newPanel = new Portraits {
                unitGO = unit,
                sprite = unit.GetComponent<UnitStateMachine>().portrait,
                duplicate = false
            };
            // Check if the panel is a duplicate so we can set the progressBar to zero later.
            foreach (Portraits portrait in portraits) {
                if (portrait.unitGO == unit) {
                    newPanel.duplicate = true;
                }
            }
            portraits.Add(newPanel);
        }

        // Add them to the TurnQueue GUI. This has to happen after GenerateQueue().
        foreach (Portraits portrait in portraits) {
            GameObject newPanel = Instantiate(turnPanelPrefab, turnQueueRT);

            Image newPanelPortrait = newPanel.transform.Find("Portrait").GetComponent<Image>();
            newPanelPortrait.sprite = portrait.sprite;

            Image progressBar = newPanel.transform.Find("ProgressBar").GetComponent<Image>();
            float calcProgress = 0f;
            if (!portrait.duplicate) {
                calcProgress = (float)portrait.unitGO.GetComponent<UnitStateMachine>().initiative / turnThreshold;
            }
            progressBar.transform.localScale = new Vector3(Mathf.Clamp(calcProgress, 0, 1), progressBar.transform.localScale.y, progressBar.transform.localScale.z);
        }
    }
}
