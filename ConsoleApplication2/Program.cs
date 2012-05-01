using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    class Program
    {
        //seeded so we get the same numbers every time
        private static Random r = new Random(1);
        //modify this to change length of test
        private const int ListLength = 5000;
        //counter to see if all threads are finished executing
        private static int _processed = 0;

        static void Main()
        {
            //create a list of random integers
            var randomIntegers = new List<int>();
            for (int i = 0; i < ListLength; i++)
                randomIntegers.Add(r.Next());

            DateTime start = DateTime.Now;
            foreach (int i in randomIntegers)
            {
                IsPrime(i);
            }
            //execution on same thread as application, which would block any other operations from happening
            WriteExecutionTime(start, "Main thread");

            start = DateTime.Now;
            _processed = 0;
            foreach (int i in randomIntegers)
            {
                Thread t = new Thread(IsPrimeObject);
                t.Name = "IsPrime Thread";
                t.Start(i);
            }
            //threads have no default way of telling us if they're done
            //so we have to create our own polling
            while(_processed != ListLength)
            {
                Thread.Sleep(10);
            }
            WriteExecutionTime(start, "Thread");
            //notice how terribly slow threads are?  That's because there's a whole bunch
            //of overhead associated with a thread (1 mb memory footprint min, etc)
            //and they don't actually execute in parallel on different cores

            start = DateTime.Now;
            _processed = 0;
            foreach(int i in randomIntegers)
            {
                ThreadPool.QueueUserWorkItem(IsPrimeObject, i);
            }
            //same for threadpools
            while (_processed != ListLength)
            {
                Thread.Sleep(10);
            }
            WriteExecutionTime(start, "ThreadPool.QueueUserWorkItem");
            //ThreadPools work quickly because they have a handful of dedicated threads
            //that they just delegate work to (actually using Tasks as of .net 4)
            //and they can execute in parallel (thanks to tasks)

            start = DateTime.Now;
            _processed = 0;
            List<Task> tasks = new List<Task>();
            foreach(int i in randomIntegers)
            {
                Task t = new Task(IsPrimeObject, i);
                t.Start();
                tasks.Add(t);
            }
            //Task has a handy helper function to do the waiting for us
            Task.WaitAll(tasks.ToArray());
            WriteExecutionTime(start, "Task");
            //Tasks work pretty similar to ThreadPool, but with some convenience
            //methods like WaitAll

            start = DateTime.Now;
            _processed = 0;
            var loop = Parallel.ForEach(randomIntegers, IsPrime);
            //ParallelLoopResult has a IsCompleted property
            while(!loop.IsCompleted)
            {
                Thread.Sleep(10);
            }
            WriteExecutionTime(start, "Parallel.ForEach");
            //same idea as tasks, but using a convenience method to execute

            start = DateTime.Now;
            _processed = 0;
            randomIntegers.AsParallel().ForAll(IsPrime);
            //nothing for AsParallel though
            while(_processed != ListLength)
            {
                Thread.Sleep(10);
            }
            WriteExecutionTime(start, "AsParallel");
            //creates a ParallelQuery that can operate linq queries as tasks
            //and automagically combine the results

            start = DateTime.Now;
            _processed = 0;
            List<BackgroundWorker> workers = new List<BackgroundWorker>();
            foreach(int i in randomIntegers)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += IsPrimeWorker;
                worker.RunWorkerAsync(i);
                workers.Add(worker);
            }
            //we can query the background worker to see if it's executing
            while(workers.Any(w => w.IsBusy))
                Thread.Sleep(10);
            WriteExecutionTime(start, "Background Worker for each number");
            //Background Workers are similar to ThreadPool, but they give us a few
            //hooks (i.e. ProgressChanged, WorkerCompleted)

            Console.ReadLine();
        }

        private static void WriteExecutionTime(DateTime start, string type)
        {
            Console.WriteLine(string.Format("{1} Evaluation took {0}", DateTime.Now.Subtract(start), type));
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
        }

        //different delegate needed for BackgroundWorker
        private static void IsPrimeWorker(object sender, DoWorkEventArgs args)
        {
            IsPrime((int)args.Argument);
        }

        //different delegate needed for Task
        private static void IsPrimeObject(object args)
        {
            IsPrime((int)args);
        }

        //standard delegate used for AsParallel.ForAll, 
        private static void IsPrime(int data)
        {
            bool found = false;
            for (int i = 2; !found && i < Math.Sqrt(data); i++)
            {
                if (data % i == 0)
                {
                    lock (typeof(Program))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("{0} is not a prime number", data));
                        Console.ResetColor();
                        found = true;
                    }
                }
            }
            if (!found)
            {
                lock (typeof(Program))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format("{0} is a prime number", data));
                    Console.ResetColor();
                }
            }
            _processed++;
        }
    }
}
