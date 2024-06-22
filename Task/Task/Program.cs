using System.Diagnostics;
using System.IO.Compression;

internal class Program
{
    private static void Main(string[] args)
    {
        // Assync part
        Stopwatch sw = new Stopwatch();
        sw.Start();
        List<int> numbersAsynс = new List<int>();
        ReadFileAsync("10m.txt", numbersAsynс);
        Console.WriteLine("Numbers async count: " + numbersAsynс.Count);
        (int, int) longestIncreaseSequence = LongestSequenceAsync(numbersAsynс, true);
        (int, int) longestDecreaseSequence = LongestSequenceAsync(numbersAsynс, false);
        numbersAsynс.Sort();
        Console.WriteLine("Max: " + numbersAsynс[numbersAsynс.Count-1]);
        Console.WriteLine("Min: " + numbersAsynс[0]);
        Console.WriteLine("Awarage: " + AwarageAsync(numbersAsynс));
        Console.WriteLine("Median: " + Median(numbersAsynс));
        Console.WriteLine("Longest increase sequence: Length = " + longestIncreaseSequence.Item2 + ", Position = " + longestIncreaseSequence.Item1);
        Console.WriteLine("Longest decrease sequence: Length = " + longestDecreaseSequence.Item2 + ", Position = " + longestDecreaseSequence.Item1);
        sw.Stop();
        Console.WriteLine("Time of calculation:" + sw.Elapsed);
    }

    static void ReadFileAsync(string filePath, List<int> numbers)
    {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096 * 10, useAsync: true))
        using (var reader = new StreamReader(stream))
        {
            string line;
            int index = 0;

            while ((line = reader.ReadLine()) != null)
            {
                if (int.TryParse(line, out int result))
                {
                    numbers.Add(result);
                }
            }
        }
    }

    static bool Comparison(int a, int b, bool upper = true)
    {
        return upper ? a < b : a > b;
    }
    static (int, int) LongestSequenceAsync(List<int> numbers, bool increase = true)
    {
        List<(int, int, int)> firstLongests = new List<(int, int, int)> ();
        List<(int, int, int)> lastLongests = new List<(int, int, int)> ();
        List<(int, int)> totalLongests = new List<(int, int)>();
        int length = numbers.Count;
        int partitionSize = length / Environment.ProcessorCount;

        object lockObj = new object();

        Parallel.For(0, Environment.ProcessorCount, i =>
        {
            int start = i * partitionSize;
            int end = (i == Environment.ProcessorCount - 1) ? length : start + partitionSize;
            (int, int, int) firstSequnce = new(0, 0, 0);
            (int, int, int) lastSequnce = new(0, 0, 0);
            int currentPosition = start;
            int currentLength = 1;
            int maxPosition = start;
            int maxLength = 1;

            for (int j = start; j < end - 1; j++)
            {
                if (Comparison(numbers[j], numbers[j + 1], increase)) { currentLength++; }
                else { firstSequnce = new (start, currentLength, numbers[start]); currentLength = 1; break;}
            }

            for (int j = start; j < end-1; j++)
            {
                if (Comparison(numbers[j], numbers[j + 1], increase)) { currentLength++; }
                else 
                {
                    if (currentLength > maxLength) { maxLength = currentLength; maxPosition = currentPosition; }
                    currentPosition = j + 1; currentLength = 1;
                }
                if (j + 1 == end) { lastSequnce = new(currentPosition, currentLength, numbers[j+1]); }
            }

            lock (lockObj)
            {
                firstLongests.Add(firstSequnce);
                lastLongests.Add(lastSequnce);
                totalLongests.Add(new (maxPosition, maxLength));
            }
        });

        (int, int) totalLongest = new (0, 0);
        foreach ((int, int) localLongest in totalLongests)
        {
            if (totalLongest.Item2 < localLongest.Item2) { totalLongest = localLongest; }
        }
        List<(int, int)> totalLongestsMerged = new List<(int, int)>();
        for (int i = 0; i < lastLongests.Count-1; i++)
        {
            if (Comparison(lastLongests[i].Item3, firstLongests[i + 1].Item3, increase))
            {
                totalLongestsMerged.Add(new(lastLongests[i].Item1, lastLongests[i].Item2 + firstLongests[i + 1].Item2));
            }
        }
        (int, int) mergedLongest = totalLongestsMerged[0];
        foreach ((int, int) merged in  totalLongestsMerged)
        {
            if (mergedLongest.Item2 <  merged.Item2) { mergedLongest = merged; }
        }

        return totalLongest.Item2 < mergedLongest.Item2? mergedLongest : totalLongest;
    }
    static int AwarageAsync(List<int> numbers)
    {
        int totalSum = 0;
        int length = numbers.Count;
        int partitionSize = length / Environment.ProcessorCount;

        object lockObj = new object();

        Parallel.For(0, Environment.ProcessorCount, i =>
        {
            int start = i * partitionSize;
            int end = (i == Environment.ProcessorCount - 1) ? length : start + partitionSize;
            int localSum = 0;

            for (int j = start; j < end; j++)
            {
                localSum += numbers[j];
            }

            lock (lockObj)
            {
                totalSum += localSum;
            }
        });

        return totalSum / length;
    }
    static int Median(List<int> numbers)
    {
        return numbers.Count % 2 == 1 ? numbers[numbers.Count / 2] :
            (numbers[numbers.Count / 2] + numbers[numbers.Count / 2 - 1]) / 2;
    }
}