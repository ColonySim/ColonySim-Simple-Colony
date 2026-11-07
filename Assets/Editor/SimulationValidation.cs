using System;
using System.Collections.Generic;
using SimpleColony;

public static class SimulationValidation
{
    static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        ValidateSkilledPopulation();
        ValidateColonyWorkPlan();
        ValidateObjectCommands();
        ValidateModuleConfigurations();
        for (int seed = 0; seed < 30; seed++)
        {
            var world = new World(seed);
            Require(world.Pawns.Count == 4, "Starting population");
            ValidateGraph(world);
            ValidateFrontierThreat(world);
            int initialCount = world.Nodes.Count;
            // Survey successive frontier territories, exercising expansion beyond a fixed array.
            for (int step = 0; step < 40; step++)
            {
                int candidate = -1;
                for (int i = world.Nodes.Count - 1; i > 0; i--)
                    if (!world.Nodes[i].Known && world.Frontier(i)) { candidate = i; break; }
                Require(candidate >= 0, "Expansion frontier exhausted");
                world.Discover(candidate);
            }
            Require(world.Nodes.Count > initialCount && world.Nodes.Count > 32, "Graph growth");
            ValidateGraph(world);
            Require(world.Convert(0, Profession.Medic), "Conversion command");
            world.Step();
            Require(world.Pawns[0].Role == Profession.Medic, "Conversion on core");
            Require(!world.Convert(0, (Profession)99), "Invalid profession accepted");
            for (int step = 0; step < 4000; step++) world.Step();
            foreach (var node in world.Nodes)
                foreach (var deposit in node.Deposits)
                    Require(!float.IsNaN(deposit.Amount) && deposit.Amount >= 0 && deposit.Amount <= deposit.Capacity, "Invalid deposit");
            foreach (var pawn in world.Pawns)
            {
                Require(pawn.Food >= 0 && pawn.Energy >= 0 && pawn.Health >= 0, "Invalid needs");
                Require(pawn.Next < world.Nodes.Count && pawn.At < world.Nodes.Count, "Invalid location");
                if (pawn.Next >= 0) Require(Contains(world.Nodes[pawn.At].Links, pawn.Next), "Moving off road");
            }
        }
        ValidateDeterminism();
        ValidateDeposits();
        ValidateTravelAndConversion();
        ValidateEconomyCommands();
        ValidateResearchEffects();
        ValidateStatistics();
        ValidateDefenseOccupancy();
        ValidateWorkforce();
    }
    static bool Contains(IReadOnlyList<int> values, int target)
    { foreach (int value in values) if (value == target) return true; return false; }
    static void ValidateGraph(World world)
    {
        var visited = new HashSet<int> { World.Home }; var pending = new Queue<int>(); pending.Enqueue(World.Home);
        while (pending.Count > 0)
        {
            var node = world.Nodes[pending.Dequeue()]; var unique = new HashSet<int>();
            foreach (int next in node.Links)
            {
                Require(next != node.Id && next >= 0 && next < world.Nodes.Count && unique.Add(next), "Invalid edge");
                Require(Contains(world.Nodes[next].Links, node.Id), "One-way edge");
                Require(world.Travel.Length(node, world.Nodes[next]) > 0, "Zero-length edge");
                if (visited.Add(next)) pending.Enqueue(next);
            }
        }
        Require(visited.Count == world.Nodes.Count, "Disconnected graph");
    }
    static void ValidateDeterminism()
    {
        var first = new World(4271); var second = new World(4271);
        for (int i = 0; i < 2000; i++) { first.Step(); second.Step(); }
        Require(first.Nodes.Count == second.Nodes.Count, "Nondeterministic graph");
        for (int i = 0; i < first.Nodes.Count; i++)
            Require(first.Nodes[i].Threat == second.Nodes[i].Threat && first.Nodes[i].X == second.Nodes[i].X, "Nondeterministic state");
        for (int i = 0; i < first.Pawns.Count; i++)
            Require(first.Pawns[i].At == second.Pawns[i].At && first.Pawns[i].Food == second.Pawns[i].Food, "Nondeterministic pawn");
    }
    static void ValidateDeposits()
    {
        var food = new Deposit(Resource.Food, 10); var matter = new Deposit(Resource.Matter, 20);
        Require(food.Take(15) == 10 && food.Amount == 0 && matter.Amount == 20, "Independent depletion");
    }
    static void ValidateSkilledPopulation()
    {
        var world = new World(42, Mechanics.All, population: new SkilledPawnModel());
        var other = new World(42, Mechanics.All, population: new SkilledPawnModel());
        var pawn = world.Pawns[0];
        Require(new World(42).Pawns[0].Skills == null, "Simple pawn model");
        for (int i = 0; i < 8; i++)
        {
            var role = (Profession)i;
            Require(pawn.Skills.Level(role) == other.Pawns[0].Skills.Level(role), "Birth skills must replay");
            Require(pawn.Skills.WorkRate(role) > 1 && pawn.Skills.Priority(role) == (pawn.Skills.Level(role)+1)/2, "Skill effects");
            Require(world.SetWorkPriority(0,role,role == Profession.Researcher ? 5 : 0), "Personal priority");
        }
        world.Step();
        Require(pawn.Role == Profession.Researcher && pawn.Target >= 0, "Highest enabled available work");
        for (int i = 0; i < 1000 && world.Stores[Resource.Knowledge] == 0; i++) world.Step();
        Require(world.Stores[Resource.Knowledge] > 0, "Skilled work actually produces knowledge");
        Require(world.SetWorkPriority(0,Profession.Researcher,null) && pawn.Skills.IsAutomatic(Profession.Researcher), "Restore automatic priority");
        float food = world.Stores[Resource.Food];
        Require(world.Recruit(Profession.Unemployed) && world.Pawns[4].Skills != null && Math.Abs(food-world.Stores[Resource.Food]-50) < .001f, "Skilled recruitment");
    }

    static void ValidateColonyWorkPlan()
    {
        var model = new SkilledPawnModel();
        var world = new World(42, population: model);
        var plan = model.WorkPlan;
        Require(!plan.ChangeMinimum(world, (Profession)99, 1), "Invalid minimum profession");
        Require(!plan.ChangeMinimum(world, Profession.Unemployed, 1), "Unemployed minimum");
        Require(!plan.ChangeMinimum(world, Profession.Gatherer, -1), "Negative minimum");
        Require(!plan.SetPriority(world, Profession.Defender, 6), "Priority bounds");
        Require(!plan.ChangeMinimum(new World(42), Profession.Gatherer, 1), "Foreign policy mutation");
        Require(plan.ChangeMinimum(world, Profession.Gatherer, 1), "Set protected minimum");
        Pawn best = world.Pawns[0];
        foreach (var pawn in world.Pawns)
        {
            if (pawn.Skills.Level(Profession.Gatherer) > best.Skills.Level(Profession.Gatherer)) best = pawn;
            // Emergency must override even a personal zero.
            world.SetWorkPriority(pawn.Id, Profession.Defender, 0);
        }
        Require(plan.SetPriority(world, Profession.Defender, 5), "Declare defense emergency");
        world.Step();
        foreach (var pawn in world.Pawns)
            Require(pawn.Role == (pawn == best ? Profession.Gatherer : Profession.Defender),
                "Emergency displaced protected specialist or ignored weak worker");
        Require(plan.ChangeMinimum(world, Profession.Gatherer, 1), "Increase minimum");
        Require(plan.ChangeMinimum(world, Profession.Gatherer, 1), "Increase minimum again");
        Require(plan.ChangeMinimum(world, Profession.Gatherer, 1), "Reserve entire population");
        Require(!plan.ChangeMinimum(world, Profession.Medic, 1), "Minima exceed living population");
        // Policy changes outside the core must first initiate a return trip.
        var worker = world.Pawns[1];
        SetFixtureProperty(worker, "At", 1);
        SetFixtureProperty(worker, "Next", -1);
        SetFixtureProperty(worker, "Food", 100f);
        SetFixtureProperty(worker, "Energy", 100f);
        world.Step();
        Require(worker.Next >= 0 && worker.Target == -1, "Reassignment must return to core");

        // Death frees a reservation; survivors still receive one protected role each.
        SetFixtureProperty(best, "Health", 0f);
        plan.Refresh(world);
        foreach (var pawn in world.Pawns)
            if (pawn.Health > 0) Require(plan.ReservedRole(world, pawn) == Profession.Gatherer, "Replace lost minimum worker");
        world.GrantDebugResources();
        Require(world.Recruit(Profession.Unemployed), "Recruit into protected workforce");
        plan.Refresh(world);
        Require(plan.ReservedRole(world, world.Pawns[4]) == Profession.Gatherer, "Newborn fills unmet minimum");

        var limitedModel = new SkilledPawnModel();
        var limited = new World(42, Mechanics.None, population: limitedModel);
        Require(!limitedModel.WorkPlan.SetPriority(limited, Profession.Defender, 5), "Disabled module profession");
        Require(new SkilledPawnModel().WorkPlan.Minimum(Profession.Gatherer) == 0, "Policy leaks across sessions");
    }

    static void ValidateObjectCommands()
    {
        var world = new World(123,Mechanics.None);
        float[] before = new float[6];
        for(int i=0;i<6;i++) before[i]=world.Stores[(Resource)i];
        world.GrantDebugResources();
        for(int i=0;i<6;i++) Require(world.Stores[(Resource)i]==before[i]+1000,"Debug resources grant");
        Require(world.Construction.Build(1,BuildingType.Quarry),"Construction order");
        float matter=world.Stores[Resource.Matter];
        Require(!world.Construction.Build(1,BuildingType.Quarry)&&world.Stores[Resource.Matter]==matter,"Duplicate construction payment");
        Require(world.Convert(0,Profession.Builder),"Assign builder");world.Step();
        SetFixtureProperty(world.Pawns[0],"At",1);
        for(int tick=0;tick<170;tick++)world.Step();
        Require(world.Nodes[1].Construction.Complete&&world.Nodes[1].Find(Resource.Matter)!=null,"Builder completes and changes territory");
        var mover=world.Pawns[1];
        Require(world.DirectPawn(mover.Id,2),"Direct move order");
        for(int tick=0;tick<1000&&mover.At!=2;tick++)world.Step();
        Require(mover.At==2&&mover.OrderedDestination==2,"Direct order reaches destination");
        world.ToggleAccess(2);world.Step();
        Require(mover.Next>=0&&mover.OrderedDestination==-1,"Closed territory evacuation: next="+mover.Next+" order="+mover.OrderedDestination+" energy="+mover.Energy+" food="+mover.Food+" blocked="+world.Nodes[2].Blocked);
        for(int tick=0;tick<1000&&mover.At!=World.Home;tick++)world.Step();
        Require(mover.At==World.Home,"Evacuation reaches core");
        var fortified=new World(123);
        fortified.GrantDebugResources();
        var target=fortified.Nodes[1];
        foreach(var node in fortified.Nodes)SetFixtureProperty(node,"Threat",0f);
        Require(fortified.Convert(0,Profession.Stabilizer),"Assign stabilizer");fortified.Step();
        var stabilizer=fortified.Pawns[0];
        Require(stabilizer.Cargo>0&&stabilizer.CargoType==Resource.Stability,"Stabilizer loads essence");
        SetFixtureProperty(stabilizer,"Next",-1);SetFixtureProperty(stabilizer,"At",1);
        for(int tick=0;tick<50;tick++)fortified.Step();
        Require(target.Fortification>0,"Essence becomes fortification");
        float protection=target.Fortification;
        typeof(Node).GetMethod("Infect",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(target,new object[]{1f});
        Require(target.Threat==0&&target.Fortification<protection,"Fortification absorbs infection");
        var combat=new World(12);
        foreach(var node in combat.Nodes)SetFixtureProperty(node,"Threat",0f);
        var defender=combat.Pawns[3];
        SetFixtureProperty(defender,"At",1);SetFixtureProperty(combat.Nodes[1],"Threat",.05f);
        combat.Step();combat.Step();
        SetFixtureProperty(combat.Nodes[2],"Threat",1f);combat.Step();
        Require(defender.Target==2,"Defender searches again after victory");
        Require(combat.SetDefenderMode(3,DefenderMode.Patrol)&&defender.OrderedDestination<0,"Patrol mode");
        Require(combat.SetDefenderMode(3,DefenderMode.Guard),"Guard mode");
        Require(combat.DirectPawn(3,2),"Assign post");
        SetFixtureProperty(defender,"Next",-1);SetFixtureProperty(defender,"At",2);SetFixtureProperty(defender,"Food",20f);
        combat.Step();Require(defender.OrderedDestination==2,"Post survives supply trip");
        bool visitedHome = false, returnedToPost = false;
        for(int tick=0;tick<2000;tick++)
        {
            combat.Step();
            visitedHome |= defender.At == World.Home;
            if(visitedHome && defender.At==2 && defender.Next<0) { returnedToPost = true; break; }
        }
        Require(returnedToPost,"Guard returns to assigned post after eating");
        int empty=0;var distribution=new World(444,Mechanics.None);
        for(int i=0;i<100;i++)if(!distribution.Nodes[i].Known)distribution.Discover(i);
        foreach(var node in distribution.Nodes)if(node.Deposits.Count==0)empty++;
        Require(empty>distribution.Nodes.Count*.4f,"World has empty land for construction");
    }

    static void ValidateModuleConfigurations()
    {
        foreach (Mechanics selection in new[] { Mechanics.None, Mechanics.Threats, Mechanics.Research, Mechanics.All })
        {
            var world = new World(4271, selection);
            bool threats = (selection & Mechanics.Threats) != 0;
            bool research = (selection & Mechanics.Research) != 0;
            bool foundThreat = false;
            foreach (var node in world.Nodes) foundThreat |= node.Threat > 0;
            Require(foundThreat == threats, "Initial threat configuration");
            typeof(Inventory).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(world.Stores, new object[] { Resource.Knowledge, 100f });
            Require(world.ResearchTransport() == research, "Research module purchase");
            for (int i = 0; i < 1000; i++) world.Step();
            if (!threats)
                foreach (var node in world.Nodes) Require(node.Threat == 0, "Disabled threat module spawned an outbreak");
        }
    }

    static void ValidateFrontierThreat(World world)
    {
        var distances = new float[world.Nodes.Count];
        for (int i = 1; i < distances.Length; i++) distances[i] = float.MaxValue;
        for (int pass = 0; pass < world.Nodes.Count - 1; pass++)
            foreach (var node in world.Nodes)
                foreach (int next in node.Links)
                    distances[next] = Math.Min(distances[next], distances[node.Id] + world.Travel.Length(node, world.Nodes[next]));
        float maximum = 0;
        foreach (float distance in distances) maximum = Math.Max(maximum, distance);
        bool found = false;
        foreach (var node in world.Nodes)
            if (node.Threat > 0) { found = true; Require(Math.Abs(distances[node.Id] - maximum) < .001f, "Threat is not at the farthest graph node"); }
        Require(found, "Missing initial threat");
    }
    static void ValidateTravelAndConversion()
    {
        var world = new World(4271);
        var pawn = world.Pawns[0];
        world.Step();
        Require(pawn.Next >= 0, "Gatherer cannot start working");
        float expectedFood = 100 - .008f - world.Travel.FoodCost(world.Nodes[pawn.At], world.Nodes[pawn.Next]);
        Require(Math.Abs(pawn.Food - expectedFood) < .001f, "Incorrect entry cost");
        float food = pawn.Food, energy = pawn.Energy;
        world.Step();
        Require(Math.Abs(pawn.Food - (food - .008f)) < .001f && pawn.Energy == energy, "Road cost charged twice");
        Require(world.Convert(0, Profession.Medic), "Conversion during journey rejected");
        world.Step();
        Require(pawn.Role == Profession.Gatherer && pawn.PendingRole == Profession.Medic, "Converted away from core");
        for (int step = 0; step < 3000 && pawn.Role != Profession.Medic; step++) world.Step();
        Require(pawn.Role == Profession.Medic && pawn.At == World.Home && !pawn.PendingRole.HasValue, "Failed to convert on return");
    }
    static void ValidateEconomyCommands()
    {
        var world = new World(4271);
        Require(!world.Recruit((Profession)99), "Invalid recruitment accepted");
        Require(world.Stores[Resource.Food] == 120, "Invalid recruitment spent resources");
        for (int i = 0; i < 4; i++) Require(world.Recruit(Profession.Gatherer), "Recruitment failed");
        Require(world.Pawns.Count == 8 && world.Stores[Resource.Food] == 20, "Recruitment accounting");
        Require(!world.Recruit(Profession.Gatherer) && world.Pawns.Count == 8 && world.Stores[Resource.Food] == 20, "Unaffordable recruitment changed state");

        world = new World(4271);
        Require(!world.ResearchTransport() && world.Travel.SpeedMultiplier == 1, "Unaffordable research changed state");
        Require(world.Recruit(Profession.Researcher), "Could not recruit researcher");
        for (int step = 0; step < 2000 && !world.CanResearch; step++) world.Step();
        float knowledge = world.Stores[Resource.Knowledge];
        float speed = world.Travel.SpeedMultiplier;
        Require(world.ResearchTransport(), "Earned research unavailable");
        Require(Math.Abs(world.Stores[Resource.Knowledge] - (knowledge - World.ResearchCost)) < .001f, "Research accounting");
        Require(Math.Abs(world.Travel.SpeedMultiplier - (speed + .15f)) < .001f, "Research effect");
        float remaining = world.Stores[Resource.Knowledge];
        Require(!world.ResearchTransport() && world.Stores[Resource.Knowledge] == remaining, "Research purchased twice");
    }
    static void ValidateResearchEffects()
    {
        foreach (var definition in ResearchProgress.Definitions)
        {
            var world = new World(4271);
            Require(!world.ResearchTechnology((ResearchType)99), "Invalid research accepted");
            // Seed resources only in the fixture, keeping production inventory mutations internal.
            typeof(Inventory).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(world.Stores, new object[] { Resource.Knowledge, 1000f });
            float roadFood = world.Travel.FoodCost(world.Nodes[0], world.Nodes[1]);
            for (int level = 1; level <= definition.MaxLevel; level++)
            {
                float knowledge = world.Stores[Resource.Knowledge];
                Require(world.ResearchTechnology(definition.Type), "Research unavailable: " + definition.Name);
                Require(world.Research.Level(definition.Type) == level, "Research level missing");
                Require(Math.Abs(world.Stores[Resource.Knowledge] - knowledge + definition.Cost * level) < .001f, "Research price mismatch");
                switch (definition.Type)
                {
                    case ResearchType.Transport: Require(Math.Abs(world.Travel.SpeedMultiplier - 1 - .15f * level) < .001f, "Transport effect"); break;
                    case ResearchType.Logistics: Require(world.CargoCapacity == 8 + 4 * level, "Logistics effect"); break;
                    case ResearchType.Rations: Require(Math.Abs(world.Travel.FoodCost(world.Nodes[0], world.Nodes[1]) - roadFood * (1 - .2f * level)) < .001f, "Rations effect"); break;
                    case ResearchType.Recovery: Require(world.RecoveryRate == 5 + 3 * level, "Recovery effect"); break;
                    case ResearchType.Defense: Require(world.DefensePower == 1 + .5f * level, "Defense effect"); break;
                }
            }
            float remaining = world.Stores[Resource.Knowledge];
            Require(world.Research.IsCompleted(definition.Type), "Research maximum missing");
            Require(!world.ResearchTechnology(definition.Type) && world.Stores[Resource.Knowledge] == remaining, "Research exceeded maximum");
            Require(!new World(4271).Research.IsCompleted(definition.Type), "Research leaked into another session");
        }
    }
    static void ValidateStatistics()
    {
        var statistics = new RuntimeStatistics();
        for (int frame = 0; frame < 8; frame++) statistics.Record(.125, 2);
        Require(statistics.FramesPerSecond == 8 && statistics.TicksPerSecond == 16, "Incorrect real-time FPS/TPS");
        for (int frame = 0; frame < 8; frame++) statistics.Record(.125, 0);
        Require(statistics.FramesPerSecond == 8 && statistics.TicksPerSecond == 0, "Paused statistics");
        statistics.Reset();
        Require(statistics.FramesPerSecond == 0 && statistics.TicksPerSecond == 0, "Statistics reset");
    }
    static void SetFixtureProperty(object target, string name, object value)
    { target.GetType().GetProperty(name).SetValue(target, value); }
    static int AssignedDefenders(World world, int node)
    {
        int count = 0;
        foreach (var pawn in world.Pawns)
            if (pawn.Health > 0 && pawn.Role == Profession.Defender && pawn.Target == node) count++;
        return count;
    }
    static void ValidateDefenseOccupancy()
    {
        var world = new World(4271);
        for (int i = 0; i < 3; i++) Require(world.Recruit(Profession.Defender), "Defender recruitment");
        foreach (var node in world.Nodes) SetFixtureProperty(node, "Threat", 0f);
        SetFixtureProperty(world.Nodes[1], "Threat", 6f);
        SetFixtureProperty(world.Pawns[4], "At", 3);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1, "Threat attracted multiple defenders");
        Require(world.Pawns[4].At == 3 && world.Pawns[4].Next == -1 && world.Pawns[4].State == Activity.Guarding, "Idle guard abandoned post");
        SetFixtureProperty(world.Nodes[2], "Threat", 6f);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1 && AssignedDefenders(world, 2) == 1, "Threat assignments not distributed");
        Pawn assigned = null;
        foreach (var pawn in world.Pawns) if (pawn.Role == Profession.Defender && pawn.Target == 1) assigned = pawn;
        SetFixtureProperty(assigned, "Health", 0f);
        world.Step();
        Require(AssignedDefenders(world, 1) == 1 && assigned.Target == -1, "Dead defender kept reservation");
        foreach (var pawn in world.Pawns) if (pawn.Health > 0 && pawn.Role == Profession.Defender && pawn.Target == 1) assigned = pawn;
        Require(world.Convert(assigned.Id, Profession.Unemployed), "Defender dismissal");
        world.Step();
        Require(AssignedDefenders(world, 1) == 1, "Dismissed defender kept reservation");
    }
    static void ValidateWorkforce()
    {
        var world = new World(4271);
        for (int i = 0; i < 3; i++) Require(world.Recruit(Profession.Unemployed), "Unemployed recruitment");
        for (int i = 0; i < 10; i++) world.Step();
        for (int i = 4; i < 7; i++) Require(world.Pawns[i].Target == -1 && world.Pawns[i].Next == -1 && world.Pawns[i].Role == Profession.Unemployed, "Unassigned worker started job");
        Require(world.AdjustProfession(Profession.Gatherer, 3), "Assign three workers");
        Require(world.ProfessionCount(Profession.Gatherer) == 5 && world.ProfessionCount(Profession.Unemployed) == 0, "Pending workforce counts");
        Require(!world.AdjustProfession(Profession.Gatherer, 3), "Assigned unavailable workers");
        world.Step();
        for (int i = 4; i < 7; i++) Require(world.Pawns[i].Role == Profession.Gatherer, "Assigned worker did not switch role at core");
        Require(world.AdjustProfession(Profession.Gatherer, -1), "Release worker");
        Require(!world.AdjustProfession(Profession.Medic, 3) && world.ProfessionCount(Profession.Unemployed) == 1, "Partial batch assignment");
        Require(world.AdjustProfession(Profession.Medic, 1), "Reassign returning worker");
        Require(world.ProfessionCount(Profession.Medic) == 1 && world.ProfessionCount(Profession.Unemployed) == 0, "Workforce accounting");
    }
}
