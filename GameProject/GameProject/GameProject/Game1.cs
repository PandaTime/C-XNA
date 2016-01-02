using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        
        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;


        // scoring support
        int score = 0;
        string scoreString;
        

        // health support
        int health = GameConstants.BURGER_INITIAL_HEALTH;
        string healthString = GameConstants.HEALTH_PREFIX +
            GameConstants.BURGER_INITIAL_HEALTH;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // enabling mouse
            //IsMouseVisible = true;
            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = GameConstants.WINDOW_HEIGHT;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.

            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content

            burgerDamage = Content.Load<SoundEffect>("sounds/BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>("sounds/BurgerDeath");
            burgerShot = Content.Load<SoundEffect>("sounds/BurgerShot");
            explosion = Content.Load<SoundEffect>("sounds/Explosion");
            teddyBounce = Content.Load<SoundEffect>("sounds/TeddyBounce");
            teddyShot = Content.Load<SoundEffect>("sounds/TeddyShot");
            // load sprite font
            font = Content.Load<SpriteFont>("Arial20");

            // load projectile and explosion sprites

            teddyBearProjectileSprite = Content.Load<Texture2D>("teddybearprojectile");
            explosionSpriteStrip = Content.Load<Texture2D>("explosion");
            frenchFriesSprite = Content.Load<Texture2D>("frenchfries");

            // add initial game objects

            burger = new Burger(Content, "burger", (GameConstants.WINDOW_WIDTH / 2), (7 * GameConstants.WINDOW_HEIGHT / 8), burgerShot);

            for (int i = 0; i < GameConstants.MAX_BEARS; i++)
            {
                SpawnBear();
            }


            // set initial health and score strings

            scoreString = GetScoreString(score);
            healthString = GameConstants.HEALTH_PREFIX + burger.healthValue;

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            // get current mouse state and update burger

            KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, keyboard);

            // update other game objects

            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears

            for (int i = 0; i < bears.Count - 1; i++)
            {
                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active)
                    {
                        CollisionResolutionInfo collision = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds, GameConstants.WINDOW_WIDTH, GameConstants.WINDOW_HEIGHT, bears[i].Velocity, bears[i].DrawRectangle, bears[j].Velocity, bears[j].DrawRectangle);
                        if (collision != null)
                        {

                            if (collision.FirstOutOfBounds)
                            {
                                bears[i].Active = false;
                                
                            }
                            else if (!collision.FirstOutOfBounds)
                            {
                                bears[i].Velocity = collision.FirstVelocity;
                                bears[i].DrawRectangle = collision.FirstDrawRectangle;
                            }
                            if (collision.SecondOutOfBounds)
                            {
                                bears[j].Active = false;
                            }
                            else if (!collision.SecondOutOfBounds)
                            {
                                bears[j].Velocity = collision.SecondVelocity;
                                bears[j].DrawRectangle = collision.SecondDrawRectangle;
                            }
                            teddyBounce.Play(0.2f, 0, 0);
                        }
                    }
                }
            }
            // check and resolve collisions between burger and teddy bears

            foreach (TeddyBear bear in bears)
            {
                if (burger.CollisionRectangle.Intersects(bear.CollisionRectangle))
                {
                    bear.Active = false;
                    //explosions.Add(new Explosion(explosionSpriteStrip, bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y, explosion));
                    explosions.Add(new Explosion(explosionSpriteStrip, bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y));

                    burger.healthValue -= GameConstants.BEAR_DAMAGE;
                    healthString = GameConstants.HEALTH_PREFIX + burger.healthValue;
                    burgerDamage.Play(0.2f, 0, 0);
                    score += GameConstants.BEAR_POINTS;
                    scoreString = GetScoreString(score);
                    CheckBurgerKill();

                }
            }

            // check and resolve collisions between burger and projectiles

            foreach (Projectile projectile in projectiles)
            {
                if (projectile.Type == ProjectileType.TeddyBear && projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    projectile.Active = false;
                    burger.healthValue -= GameConstants.TEDDY_BEAR_PROJECTILE_DAMAGE;
                    healthString = GameConstants.HEALTH_PREFIX + burger.healthValue;
                    burgerDamage.Play(0.2f, 0, 0);
                    CheckBurgerKill();
                }
            }

            // check and resolve collisions between teddy bears and projectiles

            foreach (TeddyBear bear in bears)
            {
                foreach (Projectile projecti in projectiles)
                {
                    if (projecti.Type == ProjectileType.FrenchFries && bear.CollisionRectangle.Intersects(projecti.CollisionRectangle))
                    {
                        bear.Active = false;
                        projecti.Active = false;
                        score += GameConstants.BEAR_POINTS;
                        scoreString = GetScoreString(score);
                        //explosions.Add(new Explosion(explosionSpriteStrip, bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y, explosion));
                        explosions.Add(new Explosion(explosionSpriteStrip, bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y));
                    }
                }
            }

            // clean out inactive teddy bears and add new ones as necessary
            for (int i = bears.Count - 1; i >= 0; i--)
            {
                if (!bears[i].Active)
                {
                    bears.RemoveAt(i);
                }
            }
            while (bears.Count < GameConstants.MAX_BEARS)
            {

                SpawnBear();
            }
            // clean out inactive projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (!projectiles[i].Active)
                {
                    projectiles.RemoveAt(i);
                }
            }
            // clean out finished explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                if (explosions[i].Finished)
                {
                    explosions.RemoveAt(i);
                }
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);


            spriteBatch.Begin();

            // draw game objects
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                if (bear.Active)
                {
                    bear.Draw(spriteBatch);
                }

            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // draw score and health

            spriteBatch.DrawString(font, scoreString, GameConstants.SCORE_LOCATION, Color.Black);
            spriteBatch.DrawString(font, healthString, GameConstants.HEALTH_LOCATION, Color.Black);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.FrenchFries)
            {
                return frenchFriesSprite;
            }
            else
            {
                return teddyBearProjectileSprite;
            }
        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);

        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location

            int x = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_WIDTH - 2 * GameConstants.SPAWN_BORDER_SIZE);
            int y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_HEIGHT - 2 * GameConstants.SPAWN_BORDER_SIZE);

            // generate random velocity

            float theta = RandomNumberGenerator.NextFloat((float)Math.PI * 2);
            float speed = GameConstants.MIN_BEAR_SPEED + RandomNumberGenerator.NextFloat(GameConstants.BEAR_SPEED_RANGE);
            float vox = speed * (float)Math.Cos(theta);
            float voy = speed * (float)Math.Sin(theta);

            Vector2 velocity = new Vector2(vox, voy);

            // create new bear

            TeddyBear newBear = new TeddyBear(Content, "teddybear", x, y, velocity, teddyBounce, teddyShot);

            // make sure we don't spawn into a collision

            while (!CollisionUtils.IsCollisionFree(newBear.DrawRectangle, GetCollisionRectangles()))
            {
                newBear.X = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_WIDTH - 2 * GameConstants.SPAWN_BORDER_SIZE);
                newBear.Y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_HEIGHT - 2 * GameConstants.SPAWN_BORDER_SIZE);

            }


            // add new bear to list

            bears.Add(newBear);


        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.healthValue == 0 && !burgerDead)
            {
                burgerDead = true;
                burgerDeath.Play();
            }
        }

        private string GetScoreString(int score)
        {
            return GameConstants.SCORE_PREFIX + score;
        }

        #endregion
    }
}