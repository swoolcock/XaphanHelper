﻿using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.XaphanHelper.Entities
{
    [Tracked(true)]
    [CustomEntity("XaphanHelper/Conveyor")]
    class Conveyor : Solid
    {
        public int conveyorSpeed;

        public int direction;

        private float Length;

        private bool vertical;

        private bool flipX;

        private string swapFlag;

        private string activeFlag;

        private string forceInactiveFlag;

        private bool swaped;

        private Coroutine moveRoutine = new();

        private Coroutine actorsMoveRoutine = new();

        private Coroutine moveSpritesRoutine = new();

        private int currentTotalActors;

        private List<Actor> actors = new();

        private List<Sprite> sprites = new();

        private Sprite bgSprite;

        private Sprite fgSprite;

        private string directory;

        public Conveyor(EntityData data, Vector2 offset) : base(data.Position + offset, 8, 8, safe: false)
        {
            conveyorSpeed = data.Int("speed", 75);
            direction = data.Int("direction", -1);
            Length = data.Float("length");
            if (data.Width != 8)
            {
                Length = data.Width;
            }
            vertical = data.Bool("vertical", false);
            flipX = data.Bool("flipX", false);
            swapFlag = data.Attr("swapFlag", "");
            activeFlag = data.Attr("activeFlag", "");
            forceInactiveFlag = data.Attr("forceInactiveFlag");
            directory = data.Attr("directory", "objects/XaphanHelper/Conveyor");
            if (string.IsNullOrEmpty(directory))
            {
                directory = "objects/XaphanHelper/Conveyor";
            }
            Collider = new Hitbox(vertical ? 8 : Length, vertical ? Length : 8);
            sprites = BuildSprite();
            Add(bgSprite = new Sprite(GFX.Game, directory + "/"));
            bgSprite.AddLoop("bgleft", "bg", 0.08f, 0);
            bgSprite.AddLoop("bgmid", "bg", 0.08f, 1);
            bgSprite.AddLoop("bgright", "bg", 0.08f, 2);
            if (vertical)
            {
                bgSprite.Rotation = (float)Math.PI / 2;
                if (flipX)
                {
                    bgSprite.FlipY = true;
                }
            }
            Add(fgSprite = new Sprite(GFX.Game, directory + "/"));
            fgSprite.AddLoop("fgleft", "fg", 0.08f, 0);
            fgSprite.AddLoop("fgright", "fg", 0.08f, 1);
            if (vertical)
            {
                fgSprite.Rotation = (float)Math.PI / 2;
                if (flipX)
                {
                    fgSprite.FlipY = true;
                }
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (string.IsNullOrEmpty(activeFlag) || SceneAs<Level>().Session.GetFlag(activeFlag))
            {
                Add(moveSpritesRoutine = new Coroutine(MoveSprites()));
            }
        }

        public List<Sprite> BuildSprite()
        {
            List<Sprite> list = new();
            for (int i = -8; i <= Length + 8; i++)
            {
                Sprite sprite = new(GFX.Game, directory + "/");
                sprite.AddLoop("belt", "belt", 0f);
                sprite.Play("belt");
                if (vertical)
                {
                    sprite.Rotation = (float)Math.PI / 2;
                    if (flipX)
                    {
                        sprite.FlipY = true;
                    }
                }
                sprite.Position = (vertical ? Vector2.UnitY : Vector2.UnitX) * i;
                list.Add(sprite);
                Add(sprite);
            }
            return list;
        }

        public override void Update()
        {
            base.Update();
            if (!moveSpritesRoutine.Active && ((string.IsNullOrEmpty(forceInactiveFlag) || !SceneAs<Level>().Session.GetFlag(forceInactiveFlag)) && (string.IsNullOrEmpty(activeFlag) || SceneAs<Level>().Session.GetFlag(activeFlag))))
            {
                Add(moveSpritesRoutine = new Coroutine(MoveSprites()));
            }
            if (!string.IsNullOrEmpty(swapFlag) && ((SceneAs<Level>().Session.GetFlag(swapFlag) && !swaped) || (!SceneAs<Level>().Session.GetFlag(swapFlag) && swaped)))
            {
                direction = -direction;
                swaped = !swaped;
            }
            if ((string.IsNullOrEmpty(forceInactiveFlag) || !SceneAs<Level>().Session.GetFlag(forceInactiveFlag)) && (string.IsNullOrEmpty(activeFlag) || SceneAs<Level>().Session.GetFlag(activeFlag)))
            {
                if (vertical)
                {
                    if (HasPlayerClimbing())
                    {
                        Player player = GetPlayerClimbing();
                        if (flipX)
                        {
                            if (player != null && player.Right <= Left && !moveRoutine.Active)
                            {
                                Add(moveRoutine = new Coroutine(MovePlayerRoutine(this)));
                            }
                        }
                        else
                        {
                            if (player != null && player.Left >= Right && !moveRoutine.Active)
                            {
                                Add(moveRoutine = new Coroutine(MovePlayerRoutine(this)));
                            }
                        }
                    }
                }
                else
                {
                    if (HasPlayerOnTop())
                    {
                        Player player = GetPlayerOnTop();
                        if (player != null && !moveRoutine.Active)
                        {
                            Add(moveRoutine = new Coroutine(MovePlayerRoutine(this)));
                        }
                    }
                }
                currentTotalActors = Scene.Tracker.GetEntities<Actor>().Count;
                if (currentTotalActors > 0 && !actorsMoveRoutine.Active)
                {
                    actors.Clear();
                    foreach (Actor actor in Scene.Tracker.GetEntities<Actor>())
                    {
                        actors.Add(actor);
                    }
                    Add(actorsMoveRoutine = new Coroutine(MoveActors(currentTotalActors)));
                }
            }
        }

        private IEnumerator MoveSprites()
        {
            while ((string.IsNullOrEmpty(forceInactiveFlag) || !SceneAs<Level>().Session.GetFlag(forceInactiveFlag)) && (string.IsNullOrEmpty(activeFlag) || SceneAs<Level>().Session.GetFlag(activeFlag)))
            {
                foreach (Sprite sprite in sprites)
                {
                    if (vertical)
                    {
                        sprite.Position.X = 8;
                        sprite.Position.Y += conveyorSpeed * Engine.DeltaTime * direction;
                        if (sprite.Position.Y > Length + 8)
                        {
                            sprite.Position.Y -= (Length + 8);
                        }
                        else if (sprite.Position.Y < -8)
                        {
                            sprite.Position.Y += (Length + 8);
                        }
                    }
                    else
                    {
                        sprite.Position.X += conveyorSpeed * Engine.DeltaTime * direction;
                        if (sprite.Position.X > Length + 8)
                        {
                            sprite.Position.X -= (Length + 8);
                        }
                        else if (sprite.Position.X < -8)
                        {
                            sprite.Position.X += (Length + 8);
                        }
                    }
                }
                yield return null;
            }
        }

        private IEnumerator MovePlayerRoutine(Conveyor conveyor)
        {
            if (vertical)
            {
                while(conveyor.HasPlayerClimbing())
                {
                    conveyor.GetPlayerClimbing().LiftSpeed = Vector2.UnitY * conveyorSpeed * direction / 2;
                    conveyor.GetPlayerClimbing().MoveV(conveyorSpeed * Engine.DeltaTime * direction);
                    yield return null;
                }
            }
            else
            {
                while (conveyor.HasPlayerRider())
                {
                    conveyor.GetPlayerRider().LiftSpeed = Vector2.UnitX * conveyorSpeed * direction;
                    conveyor.GetPlayerRider().MoveH(conveyorSpeed * Engine.DeltaTime * direction);
                    yield return null;
                }
            }
        }

        private IEnumerator MoveActors(int currentTotalActors)
        {
            if (!vertical)
            {
                while (currentTotalActors == this.currentTotalActors)
                {
                    foreach (Actor actor in actors)
                    {
                        if (actor.GetType() != typeof(Player) && actor.GetType() != typeof(FakePlayer) && actor.GetType() != typeof(Drone) && actor.GetType() != typeof(DroneDebris) && actor.GetType() != typeof(Debris) && actor.IsRiding(this) && actor.AllowPushing)
                        {
                            actor.MoveH(conveyorSpeed * Engine.DeltaTime * direction);
                            actor.Bottom = Top;
                        }
                    }
                    yield return null;
                }
            }
        }

        public override void Render()
        {
            for (int i = 0; i < Length / 8; i++)
            {
                bgSprite.RenderPosition = Position + (vertical ? new Vector2(8, i * 8) : new Vector2(i * 8, 0));
                bgSprite.Play(i == 0 ? "bgleft" : (i > 0 && i < (Length / 8 - 1)) ? "bgmid" : "bgright");
                bgSprite.Render();
            }
            for (int i = 0; i < Length + 16; i++)
            {
                if (vertical)
                {
                    if (sprites[i].Position.Y >= 0 && sprites[i].Position.Y < (Length - 1))
                    {
                        sprites[i].Visible = true;
                        sprites[i].DrawSubrect(Vector2.Zero, new Rectangle(i % 8, 0, 1, 8));
                    }
                    else
                    {
                        sprites[i].Visible = false;
                    }
                }
                else
                {
                    if (sprites[i].Position.X >= 0 && sprites[i].Position.X < (Length - 1))
                    {
                        sprites[i].Visible = true;
                        sprites[i].DrawSubrect(Vector2.Zero, new Rectangle(i % 8, 0, 1, 8));
                    }
                    else
                    {
                        sprites[i].Visible = false;
                    }
                }
            }
            fgSprite.RenderPosition = Position + (vertical ? new Vector2(8, 0) : Vector2.Zero);
            fgSprite.Play("fgleft");
            fgSprite.Render();
            fgSprite.RenderPosition = Position + (vertical ? new Vector2(8, Length - 8) : new Vector2(Length - 8, 0));
            fgSprite.Play("fgright");
            fgSprite.Render();
        }
    }
}
