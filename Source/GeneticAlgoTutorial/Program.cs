//	code to illustrate the use of a genetic algorithm to solve the problem described
//  at www.ai-junkie.com
//	by Mat Buckland aka fup

// (roughly) translated to c# by Tim Zenner
using System;
using System.Text;

namespace GeneticAlgoTutorial
{
    // define a data structure which will define a chromosome
    public struct chromo_typ
    {
        public string bits;
        public float fitness;

        public chromo_typ(string bits, float fitness)
        {
            this.bits = bits;
            this.fitness = fitness;
        }
    };

    class Program
    {
        const float CROSSOVER_RATE = 0.7f;
        const float MUTATION_RATE = 0.001f;
        const int POP_SIZE = 100; // must be an even number
        const int CHROMO_LENGTH = 300;
        const int GENE_LENGTH = 4;
        const int MAX_ALLOWABLE_GENERATIONS = 400;

        static Random _random;

        static void Main(string[] args)
        {
            // seed random 
            _random = new Random((int)DateTime.Now.Ticks);

            while (true)
            {
                // storage for our population of chromosomes
                chromo_typ[] Population = new chromo_typ[POP_SIZE];

                // get a target number from the user. 
                Console.WriteLine("Input a target number: ");

                int target;
                if (!int.TryParse(Console.ReadLine(), out target))
                {
                    Console.WriteLine("Unable to parse, ensure you typed a number.");

                    continue;
                }

                // first create a random population, all with zero fitness
                for (int i = 0; i < POP_SIZE; i++)
                {
                    Population[i].bits = GetRandomBits(CHROMO_LENGTH);
                    Population[i].fitness = 0.0f;
                }

                int generationsRequired = 0;

                // we will set this flag if a solution has been found
                bool found = false;

                while (!found)
                {
                    // this is used during routlette wheel sampling
                    float totalFitness = 0.0f;

                    // test and update the fitness of every chromosome in the population
                    for (int i = 0; i < POP_SIZE; i++)
                    {
                        Population[i].fitness = AssignFitness(Population[i].bits, target);
                        totalFitness += Population[i].fitness;
                    }

                    // check to see if we have found any solutions (fitness will be 999)
                    for (int i = 0; i < POP_SIZE; i++)
                    {
                        if (Population[i].fitness == 999.0f)
                        {
                            Console.WriteLine("Solution found in " + generationsRequired + " generations!");

                            PrintChromo(Population[i].bits);

                            found = true;

                            break;
                        }
                    }

                    // create a new population by selecting two parents at a time and creating offspring
                    // by applying crossover and mutation.  Do this until the desired number of offspring
                    // have been created.

                    // define some temp storage for the new population we are about to create
                    chromo_typ[] temp = new chromo_typ[POP_SIZE];

                    int cPop = 0;

                    // loop until we have created POP_SIZE new chromosomes
                    while (cPop < POP_SIZE)
                    {
                        // we are going to create the new population by grabbing members of the old population
                        // two at a time via roulette wheel selection
                        string offspring1 = Roulette((int)totalFitness, Population);
                        string offspring2 = Roulette((int)totalFitness, Population);

                        // and crossover dependent on the crossover rate
                        Crossover(ref offspring1, ref offspring2);

                        // now mutate dependent on the mutation rate
                        Mutate(ref offspring1);
                        Mutate(ref offspring2);

                        // add these offspring to the new population. (assign zero as their fitness scores)
                        temp[cPop++] = new chromo_typ(offspring1, 0.0f);
                        temp[cPop++] = new chromo_typ(offspring2, 0.0f);
                    } // end loop

                    // copy temp population into main population array
                    for (int i = 0; i < POP_SIZE; i++)
                    {
                        Population[i] = temp[i];
                    }

                    ++generationsRequired;

                    // exit app if no solution found within the maximum allowable number
                    // of generations
                    if (generationsRequired > MAX_ALLOWABLE_GENERATIONS)
                    {
                        Console.WriteLine("No solutions found this run!  :(");
                        break;
                    }
                }

                Console.WriteLine("");
            }

        }

        // returns a float between 0 and 1
        static float RANDOM_NUM
        {
            get 
            {
                return GetRandomFloat(_random);
            }
        }

        static float GetRandomFloat(Random random)
        {
            return (float)random.NextDouble();
        }

        // returns a string of random 1s and 0s of the desired length
        static string GetRandomBits(int length)
        {
            string bits = ""; 
            
            for (int i = 0; i < length; i++)
            {
                if (RANDOM_NUM > 0.5f)
                    bits += "1";
                else
                    bits += "0";
            }

            return bits;
        }

        // given a string of bits and a target value this function will calculate its
        // representation and return a fitness score accordingly
        static float AssignFitness(string bits, int target_value)
        {
            // holds decimal values of gene sequence
            int[] buffer = new int[(int)(CHROMO_LENGTH / GENE_LENGTH)];

            int num_elements = ParseBits(bits, buffer);

            // ok, we have a buffer filled with valid values of: operator - number - operator - number ...
            // now we calculate what this represents
            float result = 0.0f;

            for (int i = 0; i < num_elements - 1; i += 2)
            {
                switch(buffer[i])
                {
                    case 10:
                        result += buffer[i + 1];
                        break;

                    case 11:
                        result -= buffer[i + 1];
                        break;

                    case 12:
                        result *= buffer[i + 1];
                        break;

                    case 13:
                        result /= buffer[i + 1];
                        break;
                }
            }

            // Now we calculate the fitness.  First check to see if a solution has been found
            // and assign an arbitrarily high fitness score if this is so.
            if (result == (float)target_value)
            {
                return 999.0f;
            }
            else
            {
                return 1 / (float)Math.Abs((double)(target_value - result));
            }
        }

        // given a chromosome this function will step through the genes one at a time and insert
        // the decimal values of each gene (which follow the operator -> number -> operator rule)
        // into a buffer.  Returns the number of elements in the buffer.
        static int ParseBits(string bits, int[] buffer)
        {
            // counter for buffer position
            int cBuff = 0;

            // step through bits a gene at a time until end and store decimal values
            // of valid operators and numbers. Don't forget we are looking for 
            // operator -> number -> operator -> number and so on.  We ignore the
            // unused genes 1111 and 1110

            // flag to determin if we are lookig for an operator or a number
            bool bOperator = true;

            // storage for the decimal value of currently tested gene
            int this_gene = 0;

            for (int i = 0; i < CHROMO_LENGTH; i += GENE_LENGTH)
            {
                // convert the current gene to decimal
                this_gene = BinToDec(bits.Substring(i, GENE_LENGTH));
                
                // find a gene which represents an operator
                if (bOperator)
                {
                    if ((this_gene < 10) || (this_gene > 13))
                    {
                        continue;
                    }
                    else
                    {
                        bOperator = false;
                        buffer[cBuff++] = this_gene;
                        continue;
                    }
                }
                // find a gene which represents a number
                else
                {
                    if (this_gene > 9)
                    {
                        continue;
                    }
                    else
                    {
                        bOperator = true;
                        buffer[cBuff++] = this_gene;
                        continue;
                    }
                }
            } // next gene

            // now we have to run through buffer to see if a possible divide by zero 
            // is included and delete it.  (ie a '/' followed by a '0'). We take an easy
            // way out here and just change the '/' to a '+'. This will not affect the 
            // evolution of the solution
            for (int i = 0; i < cBuff; i++)
            {
                if ((buffer[i] == 13) && (buffer[i + 1] == 0))
                {
                    buffer[i] = 10;
                }
            }

            return cBuff;
        }

        // converts a binary string into a decimal integer
        static int BinToDec(string bits)
        {
            int val = 0;
            int value_to_add = 1;

            for (int i = bits.Length; i > 0; i--)
            {
                if (bits.Substring(i - 1, 1) == "1")
                {
                    val += value_to_add;
                }

                value_to_add *= 2;
            }

            return val;
        }

        // decodes and prints a chromo to screen
        static void PrintChromo(string bits)
        {
            // holds decimal values of gene sequence
            int[] buffer = new int[(int)(CHROMO_LENGTH / GENE_LENGTH)];

            // parse the bit string
            int num_elements = ParseBits(bits, buffer);

            for (int i = 0; i < num_elements; i++)
            {
                PrintGeneSymbol(buffer[i]);
            }

            Console.WriteLine();
        }
        
        // given an integer this function outputs its symbol to the screen
        static void PrintGeneSymbol(int val)
        {
            if (val < 10)
            {
                Console.Write(val + " ");
            }
            else
            {
                switch (val)
                {
                    case 10:
                        Console.Write(" + ");
                        break;

                    case 11:
                        Console.Write(" - ");
                        break;

                    case 12:
                        Console.Write(" * ");
                        break;

                    case 13:
                        Console.Write(" / ");
                        break;
                } // end switch
            }
        }

        // selects a chromosome from the population via roulette wheel selection
        static string Roulette(int total_fitness, chromo_typ[] population)
        {
            // generate a random number between 0 & total fitness count
            float slice = (float)(RANDOM_NUM * total_fitness);

            // go through the chromosomes adding up the fitness so far
            float fitnessSoFar = 0.0f;

            for (int i = 0; i < POP_SIZE; i++)
            {
                fitnessSoFar += population[i].fitness;

                // if the fitness so far > random number return the chromo at this point
                if (fitnessSoFar >= slice)
                {
                    return population[i].bits;
                }
            }

            return "";
        }

        // dependent on the CROSSOVER_RATE this function selects a random point along the
        // length of the chromosomes and swaps all the bits after that point
        static void Crossover(ref string offspring1, ref string offspring2)
        {
            // dependent on the crossover rate
            if (RANDOM_NUM < CROSSOVER_RATE)
            {
                // create a random crossover point
                int crossover = (int)(RANDOM_NUM * CHROMO_LENGTH);

                string t1 = offspring1.Substring(0, crossover) + offspring2.Substring(crossover);
                string t2 = offspring2.Substring(0, crossover) + offspring1.Substring(crossover);

                offspring1 = t1;
                offspring2 = t2;
            }
        }

        // mutates a chromosome's bits dependent on the MUTATION_RATE
        static void Mutate(ref string bits)
        {
            // so we can index the string
            StringBuilder sbBits = new StringBuilder(bits);

            for (int i = 0; i < bits.Length; i++)
            {
                if (RANDOM_NUM < MUTATION_RATE)
                {
                    if (sbBits[i] == '1')
                    {
                        sbBits[i] = '0';
                    }
                    else
                    {
                        sbBits[i] = '1';
                    }
                }
            }

            // pushing back the changes to the input string
            bits = sbBits.ToString();
        }
    }
}
