using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using FMOD.Studio;
using System.Collections;
using System.IO;


namespace Celeste.Mod.CanyonHelper
{
    class ToggleSwapBlock : Solid
    {
        private Level level
        {
            get
            {
                return (Level)base.Scene;
            }
        }

        private DashListener dashListener;

        private EventInstance moveSfx;

        Color offColor = Color.DarkGray;
        Color onColor = Color.MediumPurple;
        Color endColor = new Color(0.65f, 0.4f, 0.4f);

        ToggleBlockLaser[] lasers;
        int laserCount;

        Sprite middleRed;

        float lerp;

        MTexture[,] nineSliceBlock;
        Vector2[] nodes;

        ToggleBlockNode[] nodeTextures;

        int nodeIndex = 0;
        bool moving = false;

        float travelSpeed = 5f;
        bool oscillate = false;
        bool returning = false;
        bool stopAtEnd = false;

        Vector2 dirVector = Vector2.Zero;

        bool stopped = false;

        string customTexturePath;

        public ToggleSwapBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
        {
            Depth = -9999;

            nodes = new Vector2[data.Nodes.Length + 1];
            nodes[0] = Position;
            for (int i = 0; i < data.Nodes.Length; i++)
            {
                nodes[i + 1] = data.NodesOffset(offset)[i];
            }

            travelSpeed = data.Float("travelSpeed", 5f);
            oscillate = data.Bool("oscillate", false);
            stopAtEnd = data.Bool("stopAtEnd", false);
            customTexturePath = data.Attr("customTexturePath", "");

            Add(dashListener = new DashListener());
            Add(new LightOcclude(0.2f));
            Add(middleRed = CanyonModule.SpriteBank.Create("toggleBlockLightRed"));
            dashListener.OnDash = OnPlayerDashed;

            MTexture mtexture;
            
            if (customTexturePath.Length > 0)
            {
                mtexture = GFX.Game[customTexturePath];
            }
            else
            {
                mtexture = GFX.Game["objects/canyon/toggleblock/block1"];
            }

            nineSliceBlock = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSliceBlock[i, j] = mtexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }

            if (oscillate || stopAtEnd || nodes.Length <= 2)
            {
                laserCount = nodes.Length - 1;
            }
            else
            {
                laserCount = nodes.Length;
            }

            lasers = new ToggleBlockLaser[laserCount];
            nodeTextures = new ToggleBlockNode[nodes.Length];
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (nodes.Length >= 2)
            {
                if (nodes.Length != 2)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        level.Add(lasers[i] = Engine.Pooler.Create<ToggleBlockLaser>().Init(nodes[i] + new Vector2(Width, Height) / 2, nodes[GetNextNode(i)] + new Vector2(Width, Height) / 2));
                    }
                }
                else
                {
                    level.Add(lasers[0] = Engine.Pooler.Create<ToggleBlockLaser>().Init(nodes[0] + new Vector2(Width, Height) / 2, nodes[1] + new Vector2(Width, Height) / 2));
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    level.Add(nodeTextures[i] = Engine.Pooler.Create<ToggleBlockNode>().Init(nodes[i] + new Vector2(Width, Height) / 2));
                }

                RecalculateLaserColor();
            }
        }

        int GetNextNode(int node)
        {
            int nextNode;
            if (!oscillate && node == nodes.Length - 1)
            {
                nextNode = 0;
            }
            else if (!oscillate)
            {
                nextNode = node + 1;
            }
            else
            {
                if (node == nodes.Length - 1)
                {
                    nextNode = node - 1;
                }
                else if (node != 0)
                {
                    nextNode = node + (returning ? -1 : 1);
                }
                else
                {
                    nextNode = 1;
                }
            }
            return nextNode;
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            if (moveSfx != null)
            Audio.Stop(moveSfx, true);
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            if (moveSfx != null)
            Audio.Stop(moveSfx, true);
        }

        private IEnumerator TriggeredMove()
        {
            if (moving || stopped)
            {
                if (stopped)
                {
                    StartShaking(0.25f);
                }
                yield break;
            }

            moving = true;

            if (nodeIndex < nodes.Length - 1 && nodeIndex != 0)
            {
                if (!returning)
                {
                    nodeIndex++;
                }
                else
                {
                    nodeIndex--;
                }
            }
            else if (nodeIndex == 0)
            {
                if (returning)
                {
                    returning = false;
                }
                nodeIndex++;
            }
            else
            {
                if (stopAtEnd)
                {
                    nodeIndex = -1;
                    stopped = true;
                    StartShaking(0.5f);
                    yield break;
                }
                else if (oscillate)
                {
                    nodeIndex--;
                    returning = true;
                }
                else
                {
                    nodeIndex = 0;
                }
            }

            middleRed.Play("moving");
            moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", Center);

            Vector2 targetPosition = nodes[nodeIndex];
            Vector2 startPosition = Position;
            //Start moving there
            dirVector = targetPosition - startPosition;
            Vector2 lerpVector = dirVector * travelSpeed;

            Player player = Scene.Tracker.GetEntity<Player>();

            while (Position != targetPosition)
            {
                MoveTo(Vector2.Lerp(startPosition, targetPosition, lerp), lerpVector);
                lerp = Calc.Approach(lerp, 1, travelSpeed * Engine.DeltaTime);
                yield return null;
            }

            lerp = 0;
            (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f, 1f, null, null);
            moving = false;
            middleRed.Play("idle");
            Audio.Stop(moveSfx, true);
            moveSfx = null;
            Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", Center);
            RecalculateLaserColor();
        }

        public override void Update()
        {
            base.Update();

            Player player = Scene.Tracker.GetEntity<Player>();
            Vector2 swapCancel = Vector2.One;

            if (player != null)
            {
                if (player.StateMachine.State == Player.StDash)
                {
                    if (player.DashDir.X != 0 && Input.Grab.Check && player.CollideCheck(this, player.Position + Vector2.UnitX * Math.Sign(player.DashDir.X)) && Math.Sign(dirVector.X) == Math.Sign(player.DashDir.X))
                    {
                        player.StateMachine.State = Player.StClimb;
                        player.Speed = Vector2.Zero;
                    }
                    if (player.CollideCheck(this, player.Position + Vector2.UnitY))
                    {
                        if (lerp > 0)
                        {
                            if (player.DashDir.X != 0f && Math.Sign(dirVector.X) == Math.Sign(player.DashDir.X))
                            {
                                player.Speed.X = swapCancel.X = 0f;
                                if (lerp >= 0.8)
                                player.StateMachine.State = Player.StNormal;
                            }
                            if (player.DashDir.Y != 0f && Math.Sign(dirVector.Y) == Math.Sign(player.DashDir.Y))
                            {
                                player.Speed.Y = swapCancel.Y = 0f;
                                if (lerp >= 0.8)
                                player.StateMachine.State = Player.StNormal;
                            }
                        }
                    }

                    player.Speed.X = player.Speed.X * swapCancel.X;
                    player.Speed.Y = player.Speed.Y * swapCancel.Y;
                }
            }
        }

        void RecalculateLaserColor()
        {
            for (int i = 0; i < laserCount; i++)
            {
                if (lasers[i].start == nodes[nodeIndex] + new Vector2(Width, Height) / 2 && lasers[i].end == nodes[GetNextNode(nodeIndex)] + new Vector2(Width, Height) / 2)
                {
                    lasers[i].color = onColor;
                }
                else if (lasers[i].end == nodes[nodeIndex] + new Vector2(Width, Height) / 2 && lasers[i].start == nodes[GetNextNode(nodeIndex)] + new Vector2(Width, Height) / 2)
                {
                    lasers[i].color = onColor;
                }
                else
                {
                    lasers[i].color = offColor;
                }
                if (stopAtEnd)
                {
                    if (i == laserCount - 1)
                    {
                        lasers[i].color = endColor;
                    }
                }
            }
        }

        private void OnPlayerDashed(Vector2 direction)
        {
            Add(new Coroutine(TriggeredMove(), true));
        }

        public override void Render()
        {
            int nextNode;
            if (!oscillate && nodeIndex == nodes.Length - 1)
            {
                nextNode = 0;
            }
            else if (!oscillate)
            {
                nextNode = nodeIndex + 1;
            }
            else
            {
                if (nodeIndex == nodes.Length - 1)
                {
                    nextNode = nodeIndex - 1;
                }
                else if (nodeIndex != 0)
                {
                    nextNode = nodeIndex + (returning ? -1 : 1);
                }
                else
                {
                    nextNode = 1;
                }
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                if (stopAtEnd && i == nodes.Length - 1)
                {
                    nodeTextures[i].color = endColor;
                }
                else if (stopAtEnd && (i == 0 || (oscillate && i == nodes.Length - 2)))
                {
                    nodeTextures[i].color = offColor;
                }
                else
                {
                    if (moving || stopped)
                    {
                        nodeTextures[i].color = offColor;
                    }
                    else
                    {
                        nodeTextures[i].color = nextNode == i ? onColor : offColor;
                    }
                }
                
            }
            DrawBlockStyle(Position + Shake, Width, Height, nineSliceBlock, middleRed, Color.White);
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color)
        {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < num - 1; i++)
            {
                ninSlice[1, 0].Draw(pos + new Vector2((float)(i * 8), 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2((float)(i * 8), height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < num2 - 1; j++)
            {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, (float)(j * 8)), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, (float)(j * 8)), Vector2.Zero, color);
            }
            for (int k = 1; k < num - 1; k++)
            {
                for (int l = 1; l < num2 - 1; l++)
                {
                    ninSlice[1, 1].Draw(pos + new Vector2((float)k, (float)l) * 8f, Vector2.Zero, color);
                }
            }
            bool flag = middle != null;
            if (flag)
            {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }
    }
}