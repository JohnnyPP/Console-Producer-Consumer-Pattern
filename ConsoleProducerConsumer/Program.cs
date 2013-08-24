using System;
using System.Collections;
using System.Threading;

//http://www.yoda.arachsys.com/csharp/threads/deadlocks.shtml
//http://bytes.com/topic/net/answers/575276-producer-consumer#post2251375
//http://stackoverflow.com/questions/1656404/c-sharp-producer-consumer

public class Test
{
    static ProducerConsumer queue;

    static void Main()
    {
        queue = new ProducerConsumer();
        new Thread(new ThreadStart(ConsumerJob)).Start();

        Random rng = new Random(0);
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine("Producing {0}", i);
            queue.Produce(i);
            Thread.Sleep(rng.Next(1000));
        }
    }

    static void ConsumerJob()
    {
        // Make sure we get a different random seed from the
        // first thread
        Random rng = new Random(1);
        // We happen to know we've only got 10 
        // items to receive
        for (int i = 0; i < 10; i++)
        {
            object o = queue.Consume();
            Console.WriteLine("\t\t\t\tConsuming {0}", o);
            Thread.Sleep(rng.Next(1000));
        }
    }
}

public class ProducerConsumer
{
    readonly object listLock = new object();
    Queue queue = new Queue();

    public void Produce(object o)
    {
        lock (listLock)
        {
            queue.Enqueue(o);

            // We always need to pulse, even if the queue wasn't
            // empty before. Otherwise, if we add several items
            // in quick succession, we may only pulse once, waking
            // a single thread up, even if there are multiple threads
            // waiting for items.            
            Monitor.Pulse(listLock);
        }
    }

    public object Consume()
    {
        lock (listLock)
        {
            // If the queue is empty, wait for an item to be added
            // Note that this is a while loop, as we may be pulsed
            // but not wake up before another thread has come in and
            // consumed the newly added object. In that case, we'll
            // have to wait for another pulse.
            while (queue.Count == 0)
            {
                // This releases listLock, only reacquiring it
                // after being woken up by a call to Pulse
                Monitor.Wait(listLock);
            }
            return queue.Dequeue();
        }
    }
}