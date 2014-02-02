using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KASResizeFix
{
    public class KASResizeFix : PartModule
    {
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor)
                return;

            Vector3 rescaleVector = new Vector3(part.rescaleFactor, part.rescaleFactor, part.rescaleFactor);
            part.FindModelTransform("model").localScale = rescaleVector;
        }
    }
}
