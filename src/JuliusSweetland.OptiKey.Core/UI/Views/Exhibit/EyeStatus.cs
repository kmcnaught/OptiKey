using System;
using System.Collections.Generic;
using System.Text;

namespace Per_FrameAnimation
{
    public class EyeStatus
    {
        public bool visible;
        public float xPos;
        public float yPos;
        public float zPos;
        public int nMissing = 0;
        
        public void Update(bool visible, float x, float y, float z)
        {
            if (visible)
            {
                this.visible = true;
                nMissing = 0;
            }
            else
            {
                ++nMissing;
            }

            if (nMissing > 5)
            {
                this.visible = false;
            }
            else if (visible)
            {                
                xPos = x;
                yPos = y;
                zPos = z;
            }                       
        }
    }

}
