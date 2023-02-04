using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class UpgradeStation : Area2D
{
    private Upgrade associatedUpgrade;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void SetUpgrade(Upgrade upgrade)
    {
        associatedUpgrade = upgrade;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void _on_body_entered(Node2D node)
    {
        var hero = node as Hero;
        if (hero == null || associatedUpgrade == null)
            return;
    }
}


public abstract class Upgrade
{
    public static List<Upgrade> GetAllViableUpgrades()
    {
        return new List<Upgrade>
            {
                new HealthUpgrade(),
                new KnockbackUpgrade(),
                new AttackDamageUpgrade(),
                new AttackReachUpgrade(),
                new AttackSpeedUpgrade(),
                new HeroSpeedUpgrade()
            }
            .Where(upgrade => upgrade.CanSpawn())
            .ToList();
    }

    public virtual bool CanSpawn()
    {
        return true;
    }

    public abstract void Activate(Hero player);
    public abstract string GetUpgradeFileName();
}

internal class HealthUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.MaxHealth += 1;
        player.Health += 1;
    }

    public override string GetUpgradeFileName()
    {
        return "healthUpgrade";
    }
}

internal class AttackSpeedUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.IncreaseAttackSpeedBy(0.4f);
        player.MultiplyCooldownBy(0.75f);
    }

    public override string GetUpgradeFileName()
    {
        return "swordSpeedUpgrade";
    }
}

internal class AttackReachUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.IncreaseReachByFactor(0.3f);
    }

    public override string GetUpgradeFileName()
    {
        return "reachUpgrade";
    }
}

internal class AttackDamageUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.Damage += 0.4f;
    }

    public override string GetUpgradeFileName()
    {
        return "damageUpgrade";
    }
}

internal class KnockbackUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.KnockbackStrength += player.KnockbackUpgradeAmount;
    }

    public override string GetUpgradeFileName()
    {
        return "knockbackUpgrade";
    }
}

internal class HeroSpeedUpgrade : Upgrade
{
    public override void Activate(Hero player)
    {
        player.Speed += player.SpeedUpgradeAmount;
    }

    public override string GetUpgradeFileName()
    {
        return "heroSpeedUpgrade";
    }
}