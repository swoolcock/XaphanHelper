﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.XaphanHelper.Controllers
{
    [Tracked(true)]
    [CustomEntity("XaphanHelper/SetStatsFlagsController")]
    class SetStatsFlagsController : Entity
    {
        public SetStatsFlagsController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {

        }
    }
}
