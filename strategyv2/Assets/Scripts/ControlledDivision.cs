using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlledDivision : Division
{
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    public ControlledDivision(Division division, DivisionController controller = null)
        : base(division, division.Controller)
    {
        
    }
}
