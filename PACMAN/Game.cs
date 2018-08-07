using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;

namespace PACMAN
{
   public interface IEdible
   {
      void GetEaten();
   }
   public abstract class Entity
   {
      protected abstract char Graphic { get; }
      protected virtual ConsoleColor Color { get; } = ConsoleColor.White;
      public Point Location { get; set; }
      public virtual bool IsSolid => false;
      public static implicit operator char(Entity theEntity) => theEntity.Graphic;
      public static implicit operator ConsoleColor(Entity theEntity) => theEntity.Color;
      public void Draw() => Draw(Location);

      public void Draw(Point theLocation)
      {
         Point aPreviousPoint = new Point(Console.CursorLeft, Console.CursorTop);
         Console.SetCursorPosition(theLocation.X, theLocation.Y);
         Console.ForegroundColor = this;
         Console.Write(this);
         Console.SetCursorPosition(aPreviousPoint.X, aPreviousPoint.Y);
      }
      public Entity(Point theLocation)
      {
         Location = theLocation;
      }
      protected Entity FindNeighbor(Directions theDirection)
      {
         switch (theDirection)
         {
            case Directions.UP:
               return LevelMap.CurrentMap.MapGrid[Location.X, Location.Y - 1];
            case Directions.DOWN:
               return LevelMap.CurrentMap.MapGrid[Location.X, Location.Y + 1];
            case Directions.LEFT:
               return LevelMap.CurrentMap.MapGrid[Location.X - 1, Location.Y];
            case Directions.RIGHT:
            default:
               return LevelMap.CurrentMap.MapGrid[Location.X + 1, Location.Y];
         }
      }
   }
   public class EmptySpot : Entity
   {
      protected override char Graphic => ' ';
      public EmptySpot(Point theLocation) : base(theLocation) { }
   }
   public class Wall : Entity
   {
      protected override char Graphic => '+';
      protected override ConsoleColor Color => ConsoleColor.DarkBlue;
      public override bool IsSolid => true;
      public Wall(Point theLocation) : base(theLocation) { }
   }
   public class Dot : Entity, IEdible
   {
      public static int NumDots = 0;
      protected override char Graphic => '.';
      protected override ConsoleColor Color => ConsoleColor.Green;
      public Dot(Point theLocation) : base(theLocation)
      {
         NumDots++;
      }
      public void GetEaten()
      {
         LevelMap.CurrentMap.MapGrid[Location.X, Location.Y] = new EmptySpot(Location);
         NumDots--;
      }
   }
   public class BigDot : Entity, IEdible
   {
      protected override char Graphic => 'o';
      protected override ConsoleColor Color => ConsoleColor.Green;
      public BigDot(Point theLocation) : base(theLocation) { }
      public void GetEaten()
      {
         LevelMap.CurrentMap.MapGrid[Location.X, Location.Y] = new EmptySpot(Location);
      }
   }
   public class Ghost : Entity, IEdible
   {
      public bool Alive { get; set; } = true;
      public static List<Ghost> Ghosts = new List<Ghost>();
      public Entity itsStandingOn;
      protected override char Graphic => '~';
      protected override ConsoleColor Color => ConsoleColor.Red;
      public Ghost(Point theStartPoint) : base(theStartPoint)
      {
         itsStandingOn = LevelMap.CurrentMap.MapGrid[Location.X, Location.Y];
      }
      public void GetEaten()
      {
         LevelMap.CurrentMap.MapGrid[Location.X, Location.Y] = itsStandingOn;
         LevelMap.CurrentMap.MapGrid[Location.X, Location.Y].Draw();
         Ghosts.Remove(this);
      }
      public void Move()
      {
         // Find a random direction to move in
         Directions aDirection = (Directions)new Random().Next(0, 4);
         // Find the Entity at the point the Ghost is trying to move into
         Entity aNextSpot = FindNeighbor(aDirection);
         // Don't move into a wall
         if (aNextSpot is Wall)
            return;
         // Replace the spot the Ghost was standing in with whatever was on the spot before the Ghost entered it
         if (itsStandingOn != null && !(itsStandingOn is Ghost))
         {
            LevelMap.CurrentMap.MapGrid[Location.X, Location.Y] = itsStandingOn;
            itsStandingOn.Draw();
         }
         if (!(itsStandingOn is Ghost))
            itsStandingOn = aNextSpot;
         
         // Moves the Ghost to the next spot
         Location = aNextSpot.Location;
         LevelMap.CurrentMap.MapGrid[Location.X, Location.Y] = this;
         // Draw the Ghost on its new spot
         Draw();
      }
   }
   public class Pacman : Entity
   {
      public bool Alive { get; set; } = true;
      private Directions itsDirection = Directions.RIGHT;
      private int itsPowerUpCounter = 0;
      /// <summary>
      /// Pacman's current graphic to be printed on the screen
      /// </summary>
      protected override char Graphic
      {
         get
         {
            switch (itsDirection)
            {
               case Directions.UP:
                  return 'v';
               case Directions.DOWN:
                  return '^';
               case Directions.LEFT:
                  return '>';
               case Directions.RIGHT:
               default:
                  return '<';
            }
         }
      }
      protected override ConsoleColor Color => itsPowerUpCounter > 0 ? ConsoleColor.Blue : ConsoleColor.Yellow;
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="theStartPoint">Starting location</param>
      public Pacman(Point theStartPoint) : base(theStartPoint) { }
      public void PlayerInput()
      {
         switch (Console.ReadKey(true).Key)
         {
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
               Move(Directions.UP);
               break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
               Move(Directions.DOWN);
               break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
               Move(Directions.LEFT);
               break;
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
               Move(Directions.RIGHT);
               break;
         }
      }
      /// <summary>
      /// Moves Pacman in a direction if possible.
      /// </summary>
      /// <param name="theDirection">The direction to move in</param>
      public void Move(Directions theDirection)
      {
         // Change Pacman's direction
         itsDirection = theDirection;

         // Find the Entity at the point Pacman is trying to move into
         Entity aNextSpot;
         switch (itsDirection)
         {
            case Directions.UP:
               aNextSpot = LevelMap.CurrentMap.MapGrid[Location.X, Location.Y - 1];
               break;
            case Directions.DOWN:
               aNextSpot = LevelMap.CurrentMap.MapGrid[Location.X, Location.Y + 1];
               break;
            case Directions.LEFT:
               aNextSpot = LevelMap.CurrentMap.MapGrid[Location.X - 1, Location.Y];
               break;
            case Directions.RIGHT:
               aNextSpot = LevelMap.CurrentMap.MapGrid[Location.X + 1, Location.Y];
               break;
            default:
               return;
         }
         
         // If Pacman trying to move into a dot or blank space
         if (!aNextSpot.IsSolid)
         {
            // If Pacman runs into something he can eat
            if (aNextSpot is IEdible anEdibleEntity)
               TryToEat(anEdibleEntity);

            Sounds.Move();

            // Draw the spot that Pacman moved out of
            LevelMap.CurrentMap.MapGrid[Location.X, Location.Y].Draw();

            // Move Pacman into the new location
            Location = aNextSpot.Location;
         }
         else
         {
            Sounds.HitWall();
         }

         if (itsPowerUpCounter > 0)
            itsPowerUpCounter--;

         // Draw Pacman at his new location
         Draw();
      }
      /// <summary>
      /// Check to see whether Pacman can currently eat the entity he has collided with, and eat it if he can
      /// </summary>
      public void TryToEat(IEdible theEntity)
      {
         if (theEntity is Ghost aGhost)
         {
            if (itsPowerUpCounter > 0)
            {
               Eat(aGhost);
               if (aGhost.itsStandingOn is IEdible anEntity)
                  Eat(anEntity);
            }
            else
            {
               Alive = false;
            }
         }
         else
            Eat(theEntity);
      }
      /// <summary>
      /// Make Pacman eat something and give him any relevant effects
      /// </summary>
      public void Eat(IEdible theEntity)
      {
         theEntity.GetEaten();
         Sounds.Eat();

         if (theEntity is BigDot)
            itsPowerUpCounter += 14;
      }
   }
   public class LevelMap
   {
      public static LevelMap CurrentMap;
      /// <summary>
      /// The actual map information
      /// </summary>
      public Entity[,] MapGrid { get; set; }
      public LevelMap()
      {
         CurrentMap = this;
      }
      public void PrintMap()
      {
         for (int row = 0; row < MapGrid.GetLength(1); row++)
         {
            for (int column = 0; column < MapGrid.GetLength(0); column++)
            {
               MapGrid[column, row].Draw();
            }
         }
      }
      public void GetMapFromFile(string theFileName)
      {
         List<string> lines = new List<string>();

         StreamReader theReader = new StreamReader(theFileName);

         while (!theReader.EndOfStream)
         {
            string line = theReader.ReadLine().Replace(" ", ""); // Remove spaces between characters
            lines.Add(line);
         }

         Entity[,] theEntities = new Entity[lines[0].Length, lines.Count];

         for (int y = 0; y < lines.Count; y++)
         {
            for (int x = 0; x < lines[y].Length; x++)
            {
               theEntities[x, y] = MakeEntity(lines[y][x], new Point(x, y));
            }
         }
         theEntities[1, 1] = new EmptySpot(new Point(1, 1));
         Dot.NumDots--;
         MapGrid = theEntities;
      }
      public static Entity MakeEntity(char theType, Point theLocation)
      {
         switch (theType)
         {
            case '+':
               return new Wall(theLocation);
            case '.':
               return new Dot(theLocation);
            case 'o':
               return new BigDot(theLocation);
            case '_':
            default:
               return new EmptySpot(theLocation);
         }
      }
   }
   class Game
   {
      public Pacman itsPacman;
      public LevelMap itsMap;
      /// <summary>
      /// Asks which level to play
      /// </summary>
      public void PromptChooseLevel()
      {
         Console.ForegroundColor = ConsoleColor.White;
         Console.Write("Choose a level (1-3): ");

         bool levelChosen = false;
         while (!levelChosen)
         {
            switch (Console.ReadKey(true).Key)
            {
               case ConsoleKey.D1:
                  itsMap.GetMapFromFile(@"P:\Devt\Katas\2_Pacman\pakman.txt");
                  levelChosen = true;
                  break;
               case ConsoleKey.D2:
                  itsMap.GetMapFromFile(@"P:\Devt\Katas\2_Pacman\pakman2.txt");
                  levelChosen = true;
                  break;
               case ConsoleKey.D3:
                  itsMap.GetMapFromFile(@"P:\Devt\Katas\2_Pacman\pakman3.txt");
                  levelChosen = true;
                  break;
            }
         }
         Console.Clear();
      }
      /// <summary>
      /// Asks if user wants to play again
      /// </summary>
      public bool PromptPlayAgain()
      {
         Console.WriteLine("Press 'F' to play again or 'Q' to quit");
         while (true)
         {
            switch (Console.ReadKey(true).Key)
            {
               case ConsoleKey.F:
                  return true;
               case ConsoleKey.Q:
                  return false;
            }
         }
      }
      /// <summary>
      /// Game Loop
      /// </summary>
      public Game()
      {
         Console.OutputEncoding = System.Text.Encoding.UTF8;
         Console.WriteLine("Hello, welcome to Pacman");
         bool aPlayAgain = true;
         while (aPlayAgain)
         {
            itsMap = new LevelMap();

            PromptChooseLevel();

            // Initialize Pacman at position (1, 1)
            itsPacman = new Pacman(new Point(1, 1));

            // Initialize a Ghost at position (1, 5)
            Ghost.Ghosts = new List<Ghost>() { new Ghost(new Point(1, 5)) };

            // Draw the map
            itsMap.PrintMap();

            // Draw Pacman onto the map
            itsPacman.Draw();

            // Draw the ghost onto the map
            foreach (Ghost aGhost in Ghost.Ghosts)
               aGhost.Draw();

            // Play the starting sound
            Sounds.Intro();

            // Hide the cursor
            Console.CursorVisible = false;

            bool theGameOver = false;
            while (!theGameOver)
            {
               // Move Pacman
               itsPacman.PlayerInput();

               // Move Ghosts and see if any touch Pacman
               for(int i = Ghost.Ghosts.Count-1; i >= 0; i--)
               {
                  Ghost.Ghosts[i].Move();
                  if (Ghost.Ghosts[i].Location == itsPacman.Location)
                     itsPacman.TryToEat(Ghost.Ghosts[i]);
               }

               // If all of the dots are eaten, end the game
               if (Dot.NumDots <= 0)
               {
                  Console.Clear();
                  theGameOver = true;
               }
               else if (!itsPacman.Alive)
               {
                  Console.Clear();
                  theGameOver = true;
               }
            }

            Console.ForegroundColor = ConsoleColor.White;

            if (itsPacman.Alive)
            {
               Console.WriteLine("You Win.\n\n");
            }
            else
            {
               Console.WriteLine("You Lose.\n\n");
            }

            // Ask if playing again
            aPlayAgain = PromptPlayAgain();
         }
      }
   }
   public static class Sounds
   {
      public static void Intro()
      {
         SoundPlayer simpleSound = new SoundPlayer(@"P:\Devt\Katas\2_Pacman\PacManStart.wav");
         //simpleSound.Play();
      }
      public static void Move() => Console.Beep(1250, 10);
      public static void Eat() => Console.Beep(8000, 10);
      public static void HitWall() => Console.Beep(37, 10);
   }
   public enum Directions
   {
      UP, DOWN, LEFT, RIGHT
   }
}