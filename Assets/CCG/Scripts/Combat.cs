using UnityEngine;
using Mirror;
using System.Collections;

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
        int oldHealth = entity.health;
        entity.health += amount;

        // Play hurt animation if entity is a FieldCard and took damage but is not dead
        if (entity is FieldCard fieldCard && amount < 0 && entity.health > 0)
        {
            if (fieldCard.cardAnimation != null)
                fieldCard.cardAnimation.PlayHurtAnimation();
        }

        // Play hurt animation on player portrait if this is a Player and took damage
        if (entity is Player playerEntity && amount < 0 && entity.health > 0)
        {
            // Local player
            if (playerEntity == Player.localPlayer)
            {
                var portrait = GameObject.FindObjectOfType<UIPortrait>();
                if (portrait != null && portrait.playerType == PlayerType.PLAYER)
                    portrait.PlayHurtAnimationUI();
            }
            // Enemy player
            else if (Player.localPlayer != null && playerEntity == Player.localPlayer.enemyInfo.data)
            {
                var portraits = GameObject.FindObjectsOfType<UIPortrait>();
                foreach (var portrait in portraits)
                {
                    if (portrait.playerType == PlayerType.ENEMY)
                    {
                        portrait.PlayHurtAnimationUI();
                        break;
                    }
                }
            }
        }

        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            if (entity.health <= 0)
            {
                entity.health = 0;
                Debug.Log("Entity " + entity.gameObject.name + " health is now zero or less.  Calling RpcDie.");
                // Play death animation if FieldCard, then destroy
                if (entity is FieldCard fc && fc.cardAnimation != null)
                {
                    fc.StartCoroutine(PlayDeathAndDestroy(fc));
                }
                else
                {
                    entity.RpcDie();
                }
            }
        }
        else
        {
            // Online mode: Check if health is <= 0 and trigger death
            if (entity.health <= 0)
            {
                Debug.Log("Entity " + entity.gameObject.name + " health is now zero or less.  Calling RpcDie.");
                // Play death animation if FieldCard, then destroy
                if (entity is FieldCard fc && fc.cardAnimation != null)
                {
                    fc.StartCoroutine(PlayDeathAndDestroy(fc));
                }
                else
                {
                    entity.RpcDie();
                }
                // Restart server if this is a player and we are the server
                if (NetworkServer.active && entity is Player)
                {
                    var nm = (NetworkManagerCCG)NetworkManager.singleton;
                    nm.RestartServer();
                }
            }
            Debug.Log("Entity " + entity.gameObject.name + " health changed to " + entity.health);
        }
    }

    private IEnumerator PlayDeathAndDestroy(FieldCard fc)
    {
        fc.cardAnimation.PlayDeathAnimation();
        yield return new WaitForSeconds(1.0f); // Wait for animation to finish (adjust as needed)
        fc.RpcDie();
    }

    [Command(ignoreAuthority = true)]
    public void CmdIncreaseWaitTurn()
    {
        entity.waitTurn++;
    }
}