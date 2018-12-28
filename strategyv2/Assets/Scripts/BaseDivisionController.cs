﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDivisionController : MonoBehaviour
{
    public Division AttachedDivision;
    [SerializeField]
    protected GameObject DivisionDisplayContainer;

    public void Display(bool isDisplaying)
    {
        DivisionDisplayContainer.SetActive(isDisplaying);
    }

    public virtual void SelectDivision()
    {

    }
}