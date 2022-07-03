
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace BountyHunter.Data.Scripts.Definitions
{

    public class MyFactionNameDefinitionBounty : MyFactionNameDefinition
    {
        
        public MyModFactionNameTypeEnum Type;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            MyObjectBuilder_FactionNameDefinitionBounty myObjectBuilder_FactionNameDefinition = builder as MyObjectBuilder_FactionNameDefinitionBounty;
            if (myObjectBuilder_FactionNameDefinition != null)
            {
                LanguageId = myObjectBuilder_FactionNameDefinition.LanguageId;
                Type = myObjectBuilder_FactionNameDefinition.Type;
                Names = myObjectBuilder_FactionNameDefinition.Names;
                Tags = myObjectBuilder_FactionNameDefinition.Tags;
            }
        }
    }


    public enum MyModFactionNameTypeEnum
    {
        First,
        Miner,
        Trader,
        Builder,
        Bounty
    }

    public class MyObjectBuilder_FactionNameDefinitionBounty : MyObjectBuilder_FactionNameDefinition
    {
        
        public MyModFactionNameTypeEnum Type;

        public override string ToString()
        {
            
            return base.ToString();
        }
    }
}




