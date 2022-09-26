namespace RustyDTO.Supports;

public record struct ArmyPowerConfig(double Damage, double Defense, double Speed)
{
    public double ToScore(ArmyPowerConfig influence)
    {
        return (Damage * influence.Damage + Defense * influence.Defense + Speed * influence.Speed) / (influence.Damage + influence.Defense + influence.Speed);
    }
}