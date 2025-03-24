using UnityEngine;
using Mirror;

public class Combat : NetworkBehaviour
{
    [Header("Entity")]
    public Entity entity;

    [Command(ignoreAuthority = true)]
    public void CmdChangeMana(int amount)
    {
        // Increase mana by amount. If 3, increase by 3. If -3, reduce by 3.
        if (entity is Player) entity.GetComponent<Player>().mana += amount;
    }

    [Command(ignoreAuthority = true)]
    public void CmdChangeStrength(int amount)
    {
        // Increase mana by amount. If 3, increase by 3. If -3, reduce by 3.
        entity.strength += amount;
    }

    [Command(ignoreAuthority = true)]
    public void CmdChangeHealth(int amount)
    {
        entity.health += amount;

        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            if (entity.health <= 0)
            {
                entity.health = 0;
                Debug.Log("Entity " + entity.gameObject.name + " health is now zero or less.  Calling RpcDie.");
                entity.RpcDie();
            }
        }
        else
        {
            // Online mode: Check if health is <= 0 and trigger death
            if (entity.health <= 0)
            {
                Debug.Log("Entity " + entity.gameObject.name + " health is now zero or less.  Calling RpcDie.");
                entity.RpcDie();
            }
            Debug.Log("Entity " + entity.gameObject.name + " health changed to " + entity.health);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdIncreaseWaitTurn()
    {
        entity.waitTurn++;
    }
}