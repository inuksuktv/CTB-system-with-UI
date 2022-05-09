using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Attack")]
public class Attack : ScriptableObject
{
    public string attackName, attackDescription;
    public int fireTokens, waterTokens, earthTokens, airTokens;
    public float attackDamage;
}