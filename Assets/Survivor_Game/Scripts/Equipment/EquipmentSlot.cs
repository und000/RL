/// <summary>
/// 의체에 달 수 있는 부위. 부위마다 하나씩만 달 수 있고,
/// 새로 달면 원래 달려 있던 것이 바닥에 떨어진다.
/// </summary>
public enum EquipmentSlot
{
    /// <summary>몸을 버티는 뼈대. 체력과 이동에 관여한다.</summary>
    Frame,
    /// <summary>겉을 덮는 판. 맞고 버티는 쪽이다.</summary>
    Plating,
    /// <summary>팔다리를 움직이는 장치. 공격과 이동에 관여한다.</summary>
    Actuator,
    /// <summary>열을 빼는 장치. 에너지 쪽에 관여한다.</summary>
    Coolant
}

public static class EquipmentSlotInfo
{
    private static readonly string[] Names = { "골격", "장갑", "구동계", "냉각계" };

    public static readonly EquipmentSlot[] All =
    {
        EquipmentSlot.Frame,
        EquipmentSlot.Plating,
        EquipmentSlot.Actuator,
        EquipmentSlot.Coolant
    };

    public static string Name(EquipmentSlot slot)
    {
        int index = (int)slot;
        return index >= 0 && index < Names.Length ? Names[index] : slot.ToString();
    }
}
