using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class StringStringDictionary : SerializableDictionary<string, string> {}

[Serializable]
public class ObjectColorDictionary : SerializableDictionary<UnityEngine.Object, Color> {}

[Serializable]
public class ColorArrayStorage : SerializableDictionary.Storage<Color[]> {}

[Serializable]
public class StringColorArrayDictionary : SerializableDictionary<string, Color[], ColorArrayStorage> {}

[Serializable]
public class MyClass
{
    public int i;
    public string str;
}
/*
[Serializable]
public class QuaternionMyClassDictionary : SerializableDictionary<Quaternion, MyClass> {}

[Serializable]
public class IntDivisionDictionary : SerializableDictionary<int, Division>
{
    public IntDivisionDictionary()
    {

    }

    public IntDivisionDictionary(IntDivisionDictionary dict)
      : base(dict)
    {

    }
}

[Serializable]
public class IntRememberedDivisionDictionary : SerializableDictionary<int, RememberedDivision>
{
    public IntRememberedDivisionDictionary()
    {

    }

    public IntRememberedDivisionDictionary(IntRememberedDivisionDictionary dict)
      :  base(dict)
    {

    }
}
*/