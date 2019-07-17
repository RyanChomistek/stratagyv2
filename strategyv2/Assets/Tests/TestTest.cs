using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void Test()
        {
            
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TestTestWithEnumeratorPasses()
        {
            Debug.Log(ZoneDisplayManager.Instance);
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return new WaitForSeconds(3);
            // Use the Assert class to test conditions
            var zone1 = ZoneDisplayManager.Instance.CreateZoneDisplay();
            zone1.Change(new Vector3(55, 55), new Vector3(60, 60), 0);
        }
    }
}
