using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptOrder : Attribute {
    public int order;

    public ScriptOrder(int order) {
        this.order = order;
    }
}
