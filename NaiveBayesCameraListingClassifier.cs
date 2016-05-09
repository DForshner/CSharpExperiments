using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Using a naïve Bayes' classifier to determine if a given posting is a camera or accessory.
//
// Steps:
// 1) Create a preliminary training set using a heuristic based classifier.  Only a randomly selected subset of the listings should be used
// to build the training set. As there are far more camera listings than accessory listings, it's important to reflect that ratio in the training set.
// 2) Manually go through the training data to remove any entries.  Try to remove incorrect classifications in pairs to keep the ratio of camera listings to accessory listings the same.
// 3) Run the Bayes classifier using the training set you created.  To not be considered a camera a listing must contain more than 2 "accessory" words and more than 90% of the words need to be "camera" words.

// Results legend: +[camera term], -[accessory term], neutral

// Positive camera classifications:
// +fujifilm +finepix +z70 +12 +mp +digital +camera +with +5x +optical +zoom +and +2 +7 +inch +lcd +bronze [Camera]
// +canon +digital +rebel +xs +10 +1mp +digital +slr +camera +black +canon +ef +s +18 +55mm +is +lens +canon +ef +75 +300mm +iii +lens spare +lp +e5 +battery +4gb +card +gadget +bag [Camera]
// +pentax +k +x +12 +4 +mp +digital +slr +with +2 +7 +inch +lcd +and +18 +55mm +f +3 +5 +5 +6 +al +and +50 +200mm +f +4 +5 +6 +ed +lenses +black [Camera]
// +vivitar -vivicam vx029bl +10 +1mp +digital +camera +blue [Camera]
// +sony dsct2b +digital +camera +black +8 +1mp +3x +optical +zoom +2 +7 +lcd +4gb internal +memory [Camera]
// +agfaphoto +precisa 107 +digitalkamera +12 +megapixel +5 +fach +opt +zoom +6 +8 +cm +2 +7 +zoll +display +bildstabilisiert +schwarz [Camera]
// lenco +dc 511 +digitalkamera +12 +megapixel +8 +fach +digital +zoom +6 +cm +2 +4 +zoll +tft +lcd +orange [Camera]

// Negative camera classifications:
// -duragadget premium wrist +camera +carrying -strap +with +2 +year +warranty -for +panasonic +lumix -fh27 -fh25 -fp5 -fp7 -fh5 -fh2 -s3 +s1 [Accessory]
// -duragadget +blue +hard +digital +camera -carry +case +bag +with -belt -clip -for +digital +camera -belt -clip -for +casio +exilim +ex +z35 +ex z16 +ex +z2300 +ex z670 +ex +z800 +ex z350 +ex +z2000 +ex +z550 +ex z330 +ex z25 +ex z450 +ex +z280 +ex z90 +ex +h5 +ex h20g +ex +g1 [Accessory]
// cushioned neo absorption +camera -strap -for +nikon +canon +pentax +panasonic +olympus +fujifilm +kodak +sony +and +more +digital +slr -cameras +card +reader +included [Accessory]
// +advanced prime time acessory +package -for the +sony +alpha +dslr a500 +dslr a550 +dslr +a100 +dslr a200 +dslr a300 +dslr a350 +dslr a700 +dslr a900 +kit +includes +16gb +high +speed +memory +card +2 +extended +life +batteries +rapid +ac +dc +charger dedicated ttl +flash +wide +angle +lens +2x +telephoto +lens +filter +kit -flower +lens -hood +deluxe +carrying +case +more +all +lenses will fit the following +sony +lenses +18 +70mm +18 +55mm +75 +300mm +55 +200mm 50mm 100mm [Accessory]
// -sigmatek -dn -1200 +12 +1 -multimedia +digital +photo -frame +cleaning -applicator -for +digital +photo -frames -ds -240 +2 +4 +mini +digital +photo -frame +2 -covers [Accessory]
// -sigmatek -ds 740 +7 +digital +photo -frame +black -ds -240 +2 +4 +mini +digital +photo -frame +2 -covers [Accessory]
// biostek -ds -240 +2 +4 +mini +digital +photo -frame +2 -covers +cleaning -applicator -for +digital +photo -frames [Accessory]
// -fototasche -kameratasche -typ hardbox hellblau -set +mit +4 +gb +sd -karte -für +samsung st60 es55 es60 +es65 es70 [Accessory]
// -fototasche -kameratasche -als -hüfttasche -typ -adventure +mini -waist -set +mit +4 +gb +sdhc -karte -ideal -für -wander -und -fahrradtouren -für +sony -kameras [Accessory]
// -fototasche -nappaleder +schwarz -magnetverschluß +inkl +4 +gb +sdhc -karte -passend -für +panasonic +lumix +dmc tz22 ft3 tz18 +fs37 tz8 [Accessory]

// False Positives:
// kingston kingston valueram 512 mo ddr sdram pc3200 cas3 wet wipe dispenser +100 wipes dust removal spray +250 ml foam -cleaner -for screens +and keyboards +150 ml [Camera]
// -duragadget +deluxe +mini flat folding +camera +camcorder +tripod stand -for +canon +ixus 1000hs +ixus 300hs +ixus +210 +ixus 200is +ixus 130 +ixus 120is [Camera]

namespace NaiveBayesCameraListingClassifier
{
    public class Listing
    {
        public string Title { get; set; }
        public decimal Price { get; set; }
    }

    public static class Experiment
    {
        private const string LISTINGS_FILE = "listings.txt";
        private const string CAMERA_TRAINING_SET = "cameras.txt";
        private const string ACCESORIES_TRAINING_SET = "accessories.txt";

        const int TRAINING_SET_SIZE = 500;

        public static void Run()
        {
            Console.WriteLine("Select option to continue");
            Console.WriteLine("T) Generate [T]raining set from random set of listings");
            Console.WriteLine("C) [C]lassify listings using training set");
            switch(Console.ReadKey().Key)
            {
                case ConsoleKey.T:
                {
                    GenerateTrainingSet();
                    return;
                }
                case ConsoleKey.C:
                {
                    ClassifyUsingNaiveBayes();
                    return;
                }
            }
        }

        private static void ClassifyUsingNaiveBayes()
        {
            var cameraTrainingSet = ReadFile(CAMERA_TRAINING_SET).ToList();
            var cameraWordFreq = GetWordFrequency(cameraTrainingSet);
            var accessoryTrainingSet = ReadFile(ACCESORIES_TRAINING_SET).ToList();
            var accessoryWordFreq = GetWordFrequency(accessoryTrainingSet);
            var totalTerms = cameraWordFreq.Values.Sum() + accessoryWordFreq.Values.Sum();

            var listings = ReadFile(LISTINGS_FILE).Select(Parse);
            foreach (var listing in listings)
            {
                var cameraWordCount = 0;
                var accessoryWordCount = 0;

                var tokens = listing.Title.TokenizeOnWhiteSpace();
                foreach (var token in tokens)
                {
                    float tokenQ = CalculateQ(token, cameraTrainingSet.Count, cameraWordFreq, accessoryTrainingSet.Count, accessoryWordFreq, totalTerms);

                    if (tokenQ > 1)
                    {
                        cameraWordCount += 1;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("+" + token + " ");
                    }
                    else if (tokenQ < 1)
                    {
                        accessoryWordCount += 1;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("-" + token + " ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(token + " ");
                    }
                }
                Console.Write("\n");

                const float WORD_RATIO_CONSIDER_CAMERA = 0.90F;
                var totalWords = accessoryWordCount + cameraWordCount;
                const int MIN_NUM_ACCESSORY_WORDS = 3;
                var isCamera = (accessoryWordCount < MIN_NUM_ACCESSORY_WORDS) || ((float)accessoryWordCount / totalWords > WORD_RATIO_CONSIDER_CAMERA);

                Console.ForegroundColor = (isCamera) ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(isCamera ? "[Camera] " : "[Accessory] ");
            }
        }

        private static void Display(Listing listing, bool isCamera)
        {
            var color = isCamera ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;

            Console.WriteLine((isCamera ? "[Camera] " : "[Accessory] ") + listing.Title);
        }

        private static float CalculateQ(string token, int numCameraListings, IDictionary<string, int> cameraWordFreq, int numAccessoryListings, IDictionary<string, int> accessoryWordFreq, int totalTerms)
        {
            float probWordOccuringInCamera = cameraWordFreq.ContainsKey(token) ? (float)cameraWordFreq[token] / numCameraListings : 0F;
            Debug.Assert(probWordOccuringInCamera <= 1.0F, "Expected probability");

            float probWordOccuringInAccessory = accessoryWordFreq.ContainsKey(token) ? (float)accessoryWordFreq[token] / numAccessoryListings : 0F;
            Debug.Assert(probWordOccuringInAccessory <= 1.0F, "Expected probability");

            int tokenOccurances = (cameraWordFreq.ContainsKey(token) ? cameraWordFreq[token] : 0) + (accessoryWordFreq.ContainsKey(token) ? accessoryWordFreq[token] : 0);
            float probTokenOccuring = (float)tokenOccurances / totalTerms;

            if (IsNearZero(probTokenOccuring)) { return 1; }

            float probCameraListing = (float)numCameraListings / (numCameraListings + numAccessoryListings);
            float probAccessoryListing = (float)numAccessoryListings / (numCameraListings + numAccessoryListings); ;

            // Use Bayes' theorem to get probability this is camera or accessory given token
            // P(camera|token) = P(token|camera) * P(camera) / P(token)
            float probCameraGivenToken = (probWordOccuringInCamera * probCameraListing) / probTokenOccuring;

            float probAccessoryGivenToken = (probWordOccuringInAccessory * probAccessoryListing) / probTokenOccuring;

            // Q ratio to tells us how likely this a camera listing given the token occurred
            return !IsNearZero(probAccessoryGivenToken) ? (probCameraGivenToken / probAccessoryGivenToken) : float.PositiveInfinity;
        }

        private static bool IsNearZero(float v)
        {
            const float epsilon = 0.00001F;
            return v > -epsilon && v < epsilon;
        }

        /// <summary>
        /// Build an initial training set using some heuristics.
        /// This isn't perfect so you will probably want to review
        /// the generated training set before using it to classify anything.
        /// </summary>
        private static void GenerateTrainingSet()
        {
            var listings = ReadFile(LISTINGS_FILE).Select(Parse).ToList();

            var cameraTrainingSet = new List<string>();
            var accessoryTrainingSet = new List<string>();

            // Build a training set from a random subset of listings
            var rnd = new Random();
            for (var i = 0; i < TRAINING_SET_SIZE; i++)
            {
                var rndIdx = rnd.Next(listings.Count);
                var listing = listings[rndIdx];
                if (ClassifyAsCamera(listing))
                {
                    cameraTrainingSet.Add(listing.Title);
                }
                else
                {
                    accessoryTrainingSet.Add(listing.Title);
                }
            }

            File.WriteAllLines(CAMERA_TRAINING_SET, cameraTrainingSet);
            File.WriteAllLines(ACCESORIES_TRAINING_SET, accessoryTrainingSet);
        }

        private static string[] _wordsAssociatedWithAccessoryListings = new[]
        {
            // typical words
            "bag",
            "body",
            "only",
            "battery",
            "protector",
            "projector",
            "duragadget",

            // Look for phrase "for {manufacturer name}" and "for {model}"
            "for",
            "für",
            "pour"
        };

        private static string[] _wordsAssociatedWithCameraListings = new[]
        {
            // typical words
            "mp",
            "megapixel",
            "mega",
            "pixel",
            "mpix",
            "zoom",
            "compact",
            "optical",
            "stabilized",
            "digitalkamera",
            "digital",

            // Look for phrase "camera with {feature}"
            "with",
            "livré avec",
        };

        /// <summary>
        /// Try and classify using some heuristics.
        /// </summary>
        private static bool ClassifyAsCamera(Listing listing)
        {
            // 1) Examine listing price

            // Score low cost listings (probability accessories) in proportion to how near zero they are.
            const int LOW_COST = 80;
            var lowCostScore = (listing.Price < LOW_COST)
                // Score is exponential to how low the cost is
                ? -100 * (LOW_COST - (listing.Price * listing.Price) / LOW_COST) / LOW_COST
                : 0;

            // 2) Examine words.

            // Look for words typically associated with accessories
            var titleTokens = listing.Title.TokenizeOnWhiteSpace();
            const int NON_CAMERA_WORD_WEIGHT = -20;
            var accessoryWordsScore = _wordsAssociatedWithAccessoryListings
                .Where(x => titleTokens.Contains(x))
                .Select(x => NON_CAMERA_WORD_WEIGHT)
                .Sum();

            // Look for words typically associated with camera listings
            const int CAMERA_WORD_WEIGHT = 20;
            var cameraWordsScore = _wordsAssociatedWithCameraListings
                .Where(x => titleTokens.Contains(x))
                .Select(x => CAMERA_WORD_WEIGHT)
                .Sum();

            var isCameraScore = 100
                + lowCostScore / 3
                + accessoryWordsScore / 3
                + cameraWordsScore / 3;

            const int IS_CAMERA_THRESHOLD = 90;
            return isCameraScore > IS_CAMERA_THRESHOLD;
        }

        private static IDictionary<string, int> GetWordFrequency(IEnumerable<string> docs)
        {
            var freq = new Dictionary<string, int>();
            foreach(var doc in docs)
            {
                var tokens = doc.TokenizeOnWhiteSpace();
                foreach(var token in tokens)
                {
                    if (!freq.ContainsKey(token)) { freq.Add(token, 0); }
                    freq[token] += 1;
                }
            }
            return freq;
        }

        private static string[] TokenizeOnWhiteSpace(this string doc)
        {
            return doc.Split(null); // (null) splits if char.IsWhiteSpace is true
        }

        private static Listing Parse(String str)
        {
            Debug.Assert(!String.IsNullOrEmpty(str));
            var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(str);
            return new Listing
            {
                Title = Munge((string)jsonObj["title"]),
                Price = Decimal.Parse((string)jsonObj["price"]),
            };
        }

        /// <summary>
        /// Remove punctuation, multiple spaces in a row, line endings, and lowercase everything
        /// </summary>
        private static string Munge(string original)
        {
            var temp = original.ToCharArray();
            var result = new List<char>(original.Length);
            bool lastCharWasSpace = false;
            foreach (var c in temp)
            {
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    result.Add(char.ToLower(c));
                    lastCharWasSpace = false;
                }
                else if (!lastCharWasSpace)
                {
                    result.Add(' ');
                    lastCharWasSpace = true;
                }
            }

            // Remove tailing whitespace char(s)
            while (result.Any() && Char.IsWhiteSpace(result.Last()))
            {
                result.RemoveAt(result.Count - 1);
            }

            return new String(result.ToArray());
        }

        private static IEnumerable<string> ReadFile(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(file))
                {
                    while (reader.Peek() >= 0)
                    {
                        yield return reader.ReadLine();
                    }
                }
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Experiment.Run();
        }
    }
}