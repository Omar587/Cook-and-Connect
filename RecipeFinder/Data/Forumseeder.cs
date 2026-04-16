using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeFinder.Data;
using RecipeFinder.Models;
using RecipeFinder.Models.FourmModel;

namespace RecipeFinder.Data;

public static class ForumSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db          = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<Customer>>();

        // ── Guard: only seed once ──────────────────────────────────────────
        if (await db.ForumPosts.AnyAsync()) return;

        // ── 1. Create customers ───────────────────────────────────────────
        var customerData = new[]
        {
            ("marco.deluca",    "Marco",    "De Luca",    "marco.deluca@email.com"),
            ("sofia.r",         "Sofia",    "Rosenberg",  "sofia.r@email.com"),
            ("james.okafor",    "James",    "Okafor",     "james.okafor@email.com"),
            ("priya.nair",      "Priya",    "Nair",       "priya.nair@email.com"),
            ("luke.henderson",  "Luke",     "Henderson",  "luke.henderson@email.com"),
            ("amina.h",         "Amina",    "Hassan",     "amina.h@email.com"),
            ("chen.wei",        "Chen",     "Wei",        "chen.wei@email.com"),
            ("isabel.m",        "Isabel",   "Morales",    "isabel.m@email.com"),
            ("dan.kowalski",    "Dan",      "Kowalski",   "dan.kowalski@email.com"),
            ("fatima.al",       "Fatima",   "Al-Rashid",  "fatima.al@email.com"),
            ("noah.b",          "Noah",     "Brennan",    "noah.b@email.com"),
            ("yuki.tanaka",     "Yuki",     "Tanaka",     "yuki.tanaka@email.com"),
            ("rachel.v",        "Rachel",   "Voss",       "rachel.v@email.com"),
            ("omar.s",          "Omar",     "Saleh",      "omar.s@email.com"),
            ("claire.d",        "Claire",   "Dubois",     "claire.d@email.com"),
            ("arjun.patel",     "Arjun",    "Patel",      "arjun.patel@email.com"),
            ("mia.jensen",      "Mia",      "Jensen",     "mia.jensen@email.com"),
            ("carlos.fm",       "Carlos",   "Fernandez",  "carlos.fm@email.com"),
            ("emily.w",         "Emily",    "Walsh",      "emily.w@email.com"),
            ("lena.k",          "Lena",     "Kovač",      "lena.k@email.com"),
        };

        var customers = new List<Customer>();

        foreach (var (username, first, last, email) in customerData)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) { customers.Add(existing); continue; }

            var customer = new Customer
            {
                UserName  = username,
                Email     = email,
                FirstName = first,
                LastName  = last,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(customer, "Seed@1234");
            if (result.Succeeded) customers.Add(customer);
        }

        if (!customers.Any()) return;

        // ── 2. Get flairs (exclude Food Photography) ───────────────────────
        var flairs = await db.ForumFlairs
            .Where(f => !f.Name.Contains("Photography"))
            .ToListAsync();

        if (!flairs.Any()) return;

        // ── 3. Post and comment content ────────────────────────────────────
        var postTemplates = new Dictionary<string, List<(string Title, string Body)>>
        {
            ["General Cooking"] = new()
            {
                ("What's your go-to weeknight dinner?",
                 "I always struggle with weeknight meals after a long day. Lately I've been doing a simple pasta aglio e olio — garlic, olive oil, parmesan, done in 20 minutes. What do you guys default to when you're tired but still want something decent?"),

                ("The secret to a perfect omelette",
                 "Spent the last month obsessing over French omelettes. The key I found is low heat and constant movement with a rubber spatula. Never let it sit still. Also, pull it off the heat while the inside still looks slightly underdone — residual heat finishes it. Game changer."),

                ("Cast iron vs stainless steel — which do you prefer?",
                 "I've been cooking on cast iron for years but recently tried a friend's All-Clad stainless pan and was genuinely impressed by how evenly it heated. Do you stick with one or use both? What do you use each for?"),

                ("Does anyone else find cooking meditative?",
                 "After a stressful day at work, chopping vegetables and listening to music is genuinely the most relaxing part of my day. Something about having a clear goal and seeing immediate results. Anyone else feel this way?"),

                ("Best way to use up a whole chicken",
                 "Bought a whole chicken, roasted it for Sunday dinner, now I have the carcass and lots of leftover meat. What do you do? I usually make stock but looking for creative ideas for the meat itself."),

                ("How long do you let garlic cook before adding other ingredients?",
                 "I've been burning garlic for years and finally figured out you need to add it much later than I thought, and on lower heat. But I've seen recipes that say cook garlic first for 30 seconds and others that say 3-4 minutes. What's the right approach?"),

                ("Finally nailed homemade bread after 6 months of trying",
                 "Wanted to share a win. Six months of dense, sad loaves and today I finally pulled a perfect sourdough from the oven. Open crumb, crispy crust, actual flavor. The thing that changed everything was a Dutch oven and properly developed gluten. Don't give up."),

                ("What's the most useful cooking skill you've learned?",
                 "For me it was learning to taste as I go and adjust seasoning throughout the cooking process, not just at the end. Before that I was always adding salt at the table. Understanding that salt is a process, not a finish, changed everything."),

                ("Knife skills — worth taking a class?",
                 "I'm a decent home cook but my knife work is embarrassingly slow. I'm considering a local class. Has anyone done this? Did it actually make a difference in your cooking or is it one of those things you can just learn from YouTube?"),

                ("The onion problem — how do you stop crying?",
                 "I chop onions basically every day and my eyes are wrecked every time. I've tried chilling them, cutting near an open flame, wearing goggles. The only thing that actually works for me is a very sharp knife — fewer cells broken means less gas released. Any other tricks?"),

                ("Cooking for one is actually harder than cooking for four",
                 "Everything is designed for 4 servings. Half recipes don't always scale. Produce goes bad before you use it. You end up eating the same thing for five days. I've started batch cooking on Sundays and it's helped but I'd love more ideas."),

                ("What cooking YouTube channels actually changed how you cook?",
                 "Jacques Pépin's channel basically taught me that technique matters more than recipes. Kenji Alt-Lopez changed how I think about the science behind food. Who do you credit with leveling up your cooking?"),

                ("Homemade stock — is it worth the effort?",
                 "I save vegetable scraps and chicken bones in the freezer and make stock every few weeks. The difference versus store-bought is real but the time investment is significant. Do you make your own or buy it? I'm curious how many home cooks actually bother."),

                ("The best meals I've ever made were accidents",
                 "Last week I had random stuff in the fridge — some leftover roast vegetables, feta, eggs, and harissa. Threw it all in a pan with some crusty bread and it was one of the best dinners I've had in months. Do you embrace improvisation or prefer following recipes?"),

                ("How do you handle cooking for people with different dietary restrictions?",
                 "Having people over next weekend. One is vegan, one is gluten intolerant, one just doesn't eat fish. I want to make something everyone can enjoy without making three separate dishes. What's your strategy for this?"),

                ("The difference between sautéing and stir frying",
                 "I always thought these were basically the same thing but apparently they're quite different. Sautéing is medium-high heat with occasional tossing, stir frying is very high heat with constant movement. The pan and fat also differ. Did you know this distinction before learning to cook properly?"),

                ("Resting meat — how long is actually necessary?",
                 "I know you're supposed to rest steak before cutting but I've seen everything from 2 minutes to 15 minutes. I did an experiment and honestly resting for 5 minutes on a warm plate made a visible difference in how much juice stayed in the meat. What do you do?"),

                ("Do you follow recipes exactly or improvise?",
                 "I used to follow recipes to the letter and now I barely look at them. I use them for ratios and technique but the specific ingredients are suggestions to me at this point. When did you make that transition, or do you still prefer precise recipes?"),

                ("The most underrated kitchen tool",
                 "My answer is a bench scraper. I use it to transfer chopped vegetables, clean my cutting board, portion dough, scrape up fond. It cost me £4 and I reach for it constantly. What's your sleeper hit kitchen tool?"),

                ("Why does restaurant food always taste better?",
                 "Even simple dishes at a decent restaurant beat what I make at home. I think it comes down to three things: butter quantity (way more than feels reasonable), proper seasoning at every step, and extremely high heat. Am I missing something?"),
            },

            ["Recipe Share"] = new()
            {
                ("My grandmother's slow-cooked lamb shoulder — sharing the recipe",
                 "This recipe is 60 years old and I'm finally writing it down properly. You need a bone-in lamb shoulder (about 2kg), a full head of garlic, lots of rosemary, two tins of crushed tomatoes, a bottle of red wine, and patience. Season generously, sear hard on all sides, then into a covered Dutch oven at 150°C for 5 hours. It falls apart. Serve with soft polenta."),

                ("The ultimate shakshuka — my version after years of testing",
                 "I've been making shakshuka for a decade and this is my definitive version. The base is slow-cooked onions, peppers, and tomatoes with cumin, paprika, and a touch of cinnamon. The secret is adding a spoonful of harissa and a small handful of crumbled feta before the eggs go in. Serve with thick bread, never pitta."),

                ("Crispy smashed potatoes — easier than you think",
                 "Boil small potatoes until completely tender (they should almost fall apart). Drain and dry thoroughly. Smash flat on an oiled baking tray. Drizzle with more olive oil, season well, and roast at 220°C for 30-35 minutes until incredibly crispy. The boiling AND roasting is the key — you can't skip either step."),

                ("Korean-inspired beef bulgogi rice bowls",
                 "Thinly slice ribeye (partially freezing helps). Marinate in soy sauce, sesame oil, brown sugar, grated pear, garlic, and ginger for at least two hours. Cook on a screaming hot cast iron in small batches — don't crowd the pan. Serve over steamed rice with cucumber, kimchi, and a fried egg. Sesame seeds on top."),

                ("The pasta sauce that changed my life",
                 "Four ingredients: tinned San Marzano tomatoes, half an onion, butter, and salt. That's it. No garlic, no herbs. Cut the onion in half, add it to the tomatoes and butter in a saucepan, simmer for 45 minutes, remove the onion, season. Marcella Hazan's recipe and I will never make another tomato sauce. It's perfect."),

                ("Proper homemade ramen broth — 12 hour recipe",
                 "Yes, 12 hours. But most of it is hands off. Tonkotsu base: pork trotters and neck bones, blanched first, then simmered hard with kombu, dried shiitake, spring onion, and ginger. The long boil is what makes it creamy and white. Season with tare (soy, mirin, sake). Worth every minute."),

                ("Simple but stunning: pan-seared salmon with lemon butter",
                 "Pat dry, season aggressively. Start skin-side down in a cold pan with oil, then bring to medium-high heat. Press gently for the first 30 seconds to prevent curling. Cook 70% on the skin side. Flip, add butter, garlic, and lemon juice. Baste for 60 seconds. Rest 2 minutes. The texture is completely different to oven-baked."),

                ("My go-to banana bread (no mixer needed)",
                 "The riper the bananas, the better — I wait until mine are almost black. 3 bananas, 75g melted butter, 150g sugar, 1 egg, 1 tsp vanilla, 1 tsp baking soda, pinch of salt, 190g flour. Mix wet into dry, fold together gently. 180°C for 55-65 minutes. Add walnuts or chocolate chips if you want but it honestly doesn't need them."),

                ("French onion soup — the real version",
                 "The caramelization takes 45 minutes minimum. I know every recipe says 20. They're lying. Low heat, constant occasional stirring, until they're deep brown and jammy. Deglaze with brandy, add beef stock and thyme. Top with toasted baguette slices and two types of cheese — gruyere and a little parmesan. Under the broiler until bubbling."),

                ("Chicken tikka masala from scratch",
                 "Marinate chicken thighs in yogurt, lemon, and spices overnight. Grill or broil until charred. The sauce: butter, onions cooked until golden, ginger-garlic paste, tinned tomatoes, cream, and a spice blend of cumin, coriander, garam masala, paprika, and fenugreek. Add the chicken to the sauce and simmer 15 minutes. Serve with basmati and naan."),

                ("The best chocolate chip cookies I've ever made",
                 "Brown the butter first — this is not optional. Use more brown sugar than white for chewiness. Add an extra yolk. Chill the dough for 24-48 hours. Use good quality chocolate (I chop a bar rather than using chips). Bake at 175°C until just golden at the edges, still looking underdone in the center. They firm up as they cool. Add flaky salt on top."),

                ("Vegetable fried rice — better than takeaway",
                 "Day-old rice is essential — freshly cooked rice is too wet and will steam instead of fry. High heat, wok or large cast iron. Cook eggs first, set aside. Add vegetables in order of cooking time. Push everything to the sides, add rice and press into the pan. Season with soy sauce, oyster sauce, and sesame oil. Return eggs. Fold everything together."),

                ("Miso-glazed eggplant (nasu dengaku)",
                 "Halve eggplants, score the flesh in a crosshatch, brush with oil, roast cut-side down at 200°C for 20 minutes. Mix white miso, mirin, sake, and a little sugar. Flip eggplants, brush with miso glaze, back in the oven for 10 more minutes then under the broiler for 2-3 minutes until caramelized. Finish with sesame seeds and spring onion."),

                ("Classic beef stew — the recipe I come back to every winter",
                 "Chuck beef, cubed and well-seasoned. Sear in batches until deeply browned — this is where all the flavor comes from. Soffritto base (onion, celery, carrot). Tomato paste, red wine, beef stock, bay leaves, thyme. Low and slow: 160°C for 2.5-3 hours. Add potatoes in the last 45 minutes. The sauce should coat the back of a spoon."),

                ("Homemade pizza dough that actually works",
                 "500g 00 flour, 325ml warm water, 7g instant yeast, 10g salt, 15ml olive oil. Mix, knead for 10 minutes, rest 1 hour at room temperature. Divide into 4, shape into balls, refrigerate 24-72 hours (longer = more flavor). Stretch by hand — never use a rolling pin. Cook on a preheated pizza stone or steel at the highest your oven goes."),

                ("Cold brew coffee concentrate — make it at home",
                 "100g coarsely ground coffee to 1 litre of cold water. Stir, cover, and leave at room temperature for 18-24 hours. Strain through a fine mesh sieve lined with a paper filter. What you have is concentrate — dilute 1:1 with water or milk. Lasts 2 weeks in the fridge. Infinitely smoother and cheaper than buying it."),

                ("Shakshuka variations from around the world",
                 "The classic is North African/Middle Eastern but there are incredible regional variations. Turkish menemen has eggs scrambled rather than poached. Mexican huevos rancheros uses a chile-tomato salsa base. Italian uova in purgatorio uses a simple tomato and herb sauce. All based on the same principle: eggs cooked in a sauce. Which version do you know?"),

                ("Mushroom risotto — the technique most people get wrong",
                 "You don't need to stir constantly. You need to stir often enough to prevent sticking, but constant stirring breaks down the rice too much. The stock should be warm — cold stock shocks the rice and ruins the texture. Add it ladle by ladle, letting each addition absorb before adding more. Finish with butter and parmesan off the heat. The rest is about patience."),

                ("My mother's pierogi recipe — finally getting it right",
                 "The dough: 500g flour, 1 egg, 200ml sour cream, pinch of salt. Rest 30 minutes. The filling I love most: mashed potato with caramelized onion and sharp cheddar. Fold and seal carefully — wet edges help. Boil until they float plus 2 more minutes. Then pan-fry in butter with more caramelized onion until golden. Serve with sour cream."),

                ("Lemon tart that's actually worth making",
                 "Blind bake a sweet shortcrust shell until golden. The filling: 4 eggs, 200ml double cream, 150g sugar, juice and zest of 3 lemons. Whisk together and pour into the warm shell. 150°C for 25-30 minutes until just set with a slight wobble in the center. Cool completely before slicing. The contrast between the buttery pastry and sharp curd is everything."),
            },

            ["Recipe Requests"] = new()
            {
                ("Looking for a good dal recipe — preferably from someone who grew up eating it",
                 "I've tried three dal recipes from food blogs and they all taste vaguely similar and somehow flat. I'm looking for something with real depth. Does anyone have a family recipe or know what I'm missing? I suspect it's the tempering step at the end."),

                ("What can I do with a lot of zucchini?",
                 "My garden produced an absurd amount of zucchini this summer. I've done zucchini bread, grilled zucchini, and pasta. I'm running out of ideas. Please help. Serious or funny answers both welcome at this point."),

                ("Need a showstopper dessert that isn't too difficult",
                 "Having 8 people for dinner next month and I want to make something impressive for dessert but I'm not a confident baker. Ideally something I can make ahead. Any suggestions? Budget isn't a concern, time and skill are."),

                ("Looking for hearty soup recipes for winter",
                 "As the weather gets colder I want to build a repertoire of proper soups. I know minestrone and lentil soup. What are your favorites? Bonus points for anything that freezes well."),

                ("What do you make with leftover roast chicken?",
                 "Made a big roast chicken yesterday. Have most of a carcass, some leftover meat, and the roasting juices. I always default to chicken sandwiches but I want to do something more interesting. What would you make?"),

                ("Any good recipes that use tahini beyond hummus?",
                 "I bought a big jar of tahini for hummus and now I have about three quarters of it left. What else can I use it for? I know about baba ganoush but what else?"),

                ("Vegetarian dishes that are actually satisfying",
                 "My partner is vegetarian and I'm trying to expand beyond pasta and stir fry. Looking for vegetarian main dishes that feel substantial and satisfying, not just sides that lost their meat. Any recommendations?"),

                ("What's a good beginner bread recipe?",
                 "I want to start baking bread but sourdough seems too daunting as a first project. Is there a simpler bread recipe that would give me good results while I learn? I have a stand mixer if that helps."),

                ("Need ideas for a picnic that travels well",
                 "Planning a picnic next weekend, about 6 people. Need food that travels well, can sit at room temperature for a few hours, and is easy to eat without a lot of utensils. What do you bring to picnics?"),

                ("Looking for a proper recipe for pho",
                 "I've had pho at Vietnamese restaurants and want to try making it at home. Is a good homemade version actually achievable for a home cook, or is it one of those things that requires restaurant equipment? Where should I start?"),

                ("Recipe for something impressive using only pantry staples",
                 "My grocery delivery is delayed and I need to cook dinner for guests tonight using only what I have. Pasta, tinned tomatoes, dried beans, rice, olive oil, garlic, onions, eggs, parmesan, dried herbs. What would you make?"),

                ("Good recipes for a slow cooker I barely use",
                 "I have a slow cooker that's been sitting in a cupboard for two years. I'm going to finally use it. What are your best slow cooker recipes? I want to be converted to actually using it regularly."),

                ("What's a good recipe to make with saffron?",
                 "I was gifted a small tin of good quality saffron and I want to use it in something where it really shines rather than being hidden. Risotto milanese is the obvious answer — what else would you suggest?"),

                ("Simple Indian recipes for a beginner",
                 "I love Indian food but have always found the spice combinations intimidating. What are some simpler Indian recipes that would be a good starting point? Ideally ones where the techniques and spice profiles will teach me the basics."),

                ("What to make with a whole fish?",
                 "My fishmonger had beautiful whole sea bass and I bought two without really thinking about how to cook them. What would you do with whole fish? I've mostly cooked fillets before."),
            },

            ["Tips & Techniques"] = new()
            {
                ("The real reason your garlic burns and how to fix it",
                 "Garlic burns because most people add it at the wrong time on too high heat. The solution: add garlic after your onions are softened (not at the same time), lower the heat before it goes in, and watch it closely. 30-60 seconds is usually enough. It should smell sweet and fragrant, not sharp and bitter."),

                ("How to properly season cast iron — the definitive approach",
                 "After months of research and ruined pans here's what actually works. Wash with soap (yes, soap — the myth about never using soap is wrong), dry completely on the stove over heat, apply a very thin layer of flaxseed or vegetable oil, wipe off almost all of it, bake upside down at 230°C for an hour. Repeat 3-4 times. Then just cook with it regularly."),

                ("Why you should salt pasta water more than you think",
                 "Pasta water should taste like the sea — genuinely salty. Most people under-salt dramatically. The pasta absorbs the water as it cooks, and this is your only chance to season the pasta itself. A tablespoon of salt per litre of water is a reasonable starting point. The water shouldn't be unpleasantly salty, just properly seasoned."),

                ("Temperature is everything — invest in a thermometer",
                 "The single best improvement I made to my cooking was buying an instant-read thermometer. No more guessing if chicken is cooked, no more cutting into steak, no more over or underbaked bread. Internal temperature is objective truth. A decent one costs £15 and it will change how you cook protein forever."),

                ("How to make anything taste more professional: the finishing touches",
                 "Restaurants do three things home cooks skip: a squeeze of acid at the end (lemon juice, vinegar), a pat of cold butter whisked in off the heat, and finishing salt. These three additions add brightness, richness, and texture that make food taste finished rather than cooked. Practice these on any savory dish."),

                ("Understanding heat: when to use high, medium, and low",
                 "High heat: searing meat, stir frying, boiling water, getting a crust. Medium heat: sautéing vegetables, scrambled eggs, most general cooking. Low heat: caramelizing onions, gentle braises, hollandaise, anything that needs time without burning. Most beginners cook everything on high heat and wonder why things burn or dry out."),

                ("The fold vs the stir — why it matters in baking",
                 "When a recipe says fold, it means gently incorporate without developing gluten or deflating air. Use a spatula, cut through the batter, bring it up and over, rotate the bowl. If you stir a cake batter or soufflé mix, you're ruining the texture before it even goes in the oven. The fold is not decorative — it's structural."),

                ("Mise en place — prep everything before you start cooking",
                 "I resisted this for years because it felt like unnecessary work. Then I tried it. Having every ingredient chopped, measured, and ready before I turned on the heat transformed my cooking. No more burning garlic while I'm still chopping onions. No more forgetting an ingredient mid-dish. Set up your station, then cook."),

                ("How to rescue over-salted food",
                 "You can't remove salt but you can balance it. Add more of the other ingredients to dilute. Add acid (lemon, vinegar) which competes with saltiness. Add something sweet in small amounts. Add dairy (cream, yogurt, butter) which coats the palate. For soups and sauces, a raw potato is a myth — it doesn't actually absorb salt."),

                ("The right way to store fresh herbs",
                 "Soft herbs (parsley, cilantro, basil) store like flowers — trim the stems, put in a glass of water, cover loosely with a bag, keep in the fridge (except basil, which hates cold). Hard herbs (rosemary, thyme, oregano) can be rolled in a damp paper towel and refrigerated. Done right, herbs last 1-2 weeks instead of 3 days."),

                ("Deglazing — how to make a pan sauce in 5 minutes",
                 "After searing meat, you have fond (the browned bits) stuck to the pan. That's pure flavor. Remove the meat, keep the pan hot, add wine or stock, and scrape up all the brown bits with a wooden spoon. Reduce by half, add a knob of butter, season. You've made a restaurant-quality sauce from what most people would wash down the drain."),

                ("Why resting dough matters more than you think",
                 "Resting develops gluten structure without kneading, allows flour to fully hydrate, and makes the dough much easier to work with. For bread: an autolyse (mixing flour and water, resting 30 min before adding salt and yeast) makes a dramatically better dough. For pasta: resting for 30 minutes means the dough rolls out without springing back."),

                ("How to cut an onion without crying — the actual technique",
                 "Keep the root end intact — it holds the layers together and limits cell damage. Use a very sharp knife — the sharper the blade, the fewer cells you rupture, the less gas is released. Work quickly. Chill the onion beforehand. Cut away from your face. These combined make a real difference. Goggles work too but feel absurd."),

                ("Emulsification — the technique behind vinaigrettes, mayo, and hollandaise",
                 "An emulsion is fat and water combining into a stable mixture. The key is adding fat slowly to the water-based component while something acts as an emulsifier (mustard in vinaigrette, egg yolk in mayo and hollandaise). If it breaks, you can usually save it by starting fresh with a small amount and slowly whisking the broken mixture back in."),

                ("How to develop your palate and cook without recipes",
                 "Taste everything constantly. Taste raw ingredients, taste at every stage, taste before serving. Understand the five basic tastes (sweet, sour, salty, bitter, umami) and learn what adjusts each. When something tastes off, diagnose why before reaching for more salt. Most cooking problems are acid deficiency, not salt deficiency."),

                ("The difference between caramelization and the Maillard reaction",
                 "These are not the same thing. Caramelization is sugar browning from heat alone. The Maillard reaction is a chemical reaction between amino acids and sugars that creates hundreds of new flavor compounds — it's what makes bread crusts, seared meat, roasted coffee, and toasted nuts taste the way they do. It requires dry heat and happens above about 140°C."),

                ("Blanching vegetables — a technique worth learning",
                 "Blanching (brief boiling followed by ice bath) sets the color of green vegetables permanently, partially cooks vegetables before finishing them another way, and makes peeling tomatoes and stone fruits easy. The ice bath is not optional — it stops the cooking instantly. Without it you're just boiling. With it, your broccoli stays vivid green for days."),

                ("How to tell when oil is hot enough without a thermometer",
                 "For sautéing: a drop of water should evaporate instantly and sizzle. For frying: a wooden chopstick inserted should bubble steadily around it (about 170°C). A cube of bread should brown in 60 seconds for around 180°C. A piece of the actual food should sizzle immediately when it hits the oil. If it doesn't, your oil isn't ready."),

                ("Why braising is the most forgiving cooking technique",
                 "Braising (browning then slow cooking in liquid) is almost impossible to ruin. The collagen in tough cuts converts to gelatin over low heat, making the meat silky and self-basting. It's very difficult to overcook a braise — if anything, longer is usually better. It reheats beautifully and often tastes better the next day. It's the perfect weekend cooking project."),

                ("Knife sharpening — what actually works at home",
                 "A honing steel realigns the edge (do this every time you cook). A whetstone actually removes metal and resharpens (do this every few months). Electric sharpeners are convenient but remove too much metal over time. The best home setup is a mid-grit whetstone (1000) and a honing steel. Sharp knives are safer than dull ones — less pressure required, more control."),
            },

            ["Healthy & Dietary"] = new()
            {
                ("How I actually started eating more vegetables (without hating it)",
                 "I used to find vegetables boring until I realized I was cooking them badly. The game changers: roasting instead of steaming (the caramelization is everything), adding fat (olive oil, butter, tahini), adding acid (lemon, vinegar), and adding texture (seeds, nuts, breadcrumbs). Now I genuinely look forward to vegetable dishes."),

                ("Meal prep that doesn't make you dread Sunday",
                 "I keep it simple: cook one grain (usually farro or brown rice), roast a tray of vegetables, make a big batch of legumes or a protein. Then during the week I mix and match with different sauces and dressings. The key is not making complete meals — make components and keep them flexible. You won't want the same bowl five days in a row."),

                ("Protein without meat — what actually works",
                 "Legumes (lentils, chickpeas, black beans) are the foundation. Tofu and tempeh are excellent if you know how to cook them — most people don't press tofu properly or get it hot enough to develop texture. Eggs are your best friend. Greek yogurt, cottage cheese, edamame. Quinoa. Combine two sources per meal and hitting protein goals is manageable."),

                ("Reducing sugar in baking — what substitutions work",
                 "You can reduce sugar in most recipes by up to 25% without noticeably affecting texture or structure. Beyond that, texture changes significantly because sugar isn't just sweet — it affects moisture and tenderness. Ripe fruit, dates, and honey can replace sugar but bring different properties. I'd rather make the original recipe less often than a mediocre low-sugar version regularly."),

                ("The Mediterranean diet — is it actually sustainable?",
                 "I've been following Mediterranean principles for about two years. The key is it's not really a diet in the restrictive sense — it's a food culture. Lots of olive oil, vegetables, legumes, fish, a little meat, good bread, wine with food. The reason it works long-term is it's genuinely delicious and satisfying. Nothing feels like deprivation."),

                ("Gut health — what dietary changes actually made a difference",
                 "I spent a lot of money on probiotic supplements before realizing that fermented foods (kefir, kimchi, yogurt, sauerkraut) are more effective and cheaper. Increasing fiber (especially from diverse plant sources) made the biggest difference of all. More diverse plants = more diverse gut microbiome. Aim for 30 different plant foods a week."),

                ("How to make tofu actually taste good",
                 "The steps most people skip: press the tofu for at least 30 minutes to remove moisture, freeze and thaw it for a chewier texture (optional but excellent), marinate it, and cook it at high heat until genuinely crispy. Silken tofu is for smoothies and desserts. Extra-firm is for savory cooking. These are not interchangeable."),

                ("Gluten-free cooking that doesn't feel like a compromise",
                 "After cooking gluten-free for two years, here's what I've learned. Rice, corn, oat, and almond flours all behave very differently — use recipe-specific blends rather than 1:1 swaps. Xanthan gum helps with structure. Naturally gluten-free dishes (dal, risotto, most Asian cooking) are infinitely better than gluten-free versions of wheat-based dishes."),

                ("Calorie density — the concept that changed how I eat",
                 "Instead of counting calories, I started thinking about calorie density (calories per gram of food). Vegetables and fruits have very low calorie density — you can eat large volumes and feel full. Nuts and oils are very high density — small amounts, lots of calories. Building meals around low-density foods means you naturally eat less without feeling deprived."),

                ("My experience going dairy-free for three months",
                 "I expected it to be miserable and it wasn't as bad as I feared. The hardest things were cheese (there's no good substitute honestly), butter in baking (coconut oil works), and cream in sauces (cashew cream is surprisingly good). The easiest: plant milks in coffee and cereal. If you're considering it, the hardest part is the first two weeks."),

                ("High-protein vegetarian meals I actually want to eat",
                 "My current rotation: saag paneer (paneer is incredibly high protein), lentil soup with a poached egg on top, Greek salad with lots of feta and chickpeas, vegetarian chili with kidney beans and quinoa, tofu scramble with nutritional yeast. The key is not trying to replicate meat dishes — build protein-rich vegetarian dishes in their own right."),

                ("What actually happened when I cut out ultra-processed food for 30 days",
                 "Energy levels were more stable — no afternoon crashes. Sleep was noticeably better from about week two. I lost weight without tracking calories or restricting portions. The hardest part was the social aspect — eating out, other people's houses, convenience when busy. I didn't stick to it rigidly after the 30 days but it permanently changed what I reach for by default."),
            },

            ["Vegetarian & Vegan"] = new()
            {
                ("How to make a vegan dish that carnivores actually enjoy",
                 "The mistake most people make is trying to replicate meat. Instead: focus on dishes that are naturally great without meat. Mushroom ragù over pasta, properly spiced dal, roasted cauliflower with tahini and herbs, good vegetable curries. Build flavor through caramelization, umami (miso, soy, nutritional yeast), and acid. Don't apologize for it being vegan."),

                ("The umami problem in vegan cooking — how to solve it",
                 "Umami is what makes savory food taste complete and satisfying. Without meat and dairy, you have to build it deliberately. My toolkit: miso paste, soy sauce and tamari, nutritional yeast, dried mushrooms (rehydrate and use the liquid), tomato paste, roasted garlic, fermented foods, good quality olive oil. Combine two or three per dish."),

                ("Aquafaba — the vegan egg white you already have",
                 "The liquid from a tin of chickpeas whips into meringue. Genuinely. Three tablespoons equals one egg white. Whip with cream of tartar until stiff peaks form. Use for meringues, macarons, chocolate mousse, and as a binder. It behaves exactly like egg white because it has similar proteins. Pour the chickpea water down the drain no more."),

                ("My favorite vegan dinner party menu",
                 "Starter: roasted beet and walnut salad with cashew cream dressing. Main: mushroom and lentil shepherd's pie with creamy mashed potato (made with oat milk and good olive oil). Dessert: dark chocolate mousse made with aquafaba and coconut cream. Every single time I serve this, people are surprised it's vegan. The shepherd's pie especially."),

                ("The best plant milks for different uses",
                 "Oat milk: best for coffee (froths well, neutral flavor). Soy milk: best for cooking and baking (highest protein, most similar to dairy in recipes). Almond milk: good for cereal, too thin for cooking. Coconut milk (tinned, full fat): essential for curries and Asian dishes. Cashew milk: excellent for cream-based sauces. They're not interchangeable — use the right one for the job."),

                ("Vegan cheese — the homemade versions are better than store-bought",
                 "Cashew-based cream cheese is genuinely good and takes 10 minutes. Soak cashews for 4 hours, blend with lemon juice, nutritional yeast, garlic, and salt until very smooth. Ferment overnight at room temperature for a tangy flavor. The commercial vegan cheeses I've tried are mostly disappointing. The homemade versions aren't trying to replicate dairy — they're their own thing."),

                ("Seitan — the vegan protein most people overlook",
                 "Seitan (wheat gluten) has a meaty, chewy texture that tofu and legumes don't replicate. Make it from vital wheat gluten flour — mix with broth and spices, knead briefly, then simmer or bake. It takes on marinades beautifully and can be sliced, shredded, or ground. It's not for people with gluten intolerance but for everyone else it's an excellent protein source."),

                ("Building flavor in vegan dishes from the ground up",
                 "Start with aromatics properly cooked (not rushed). Add tomato paste and cook it out in the oil until it darkens. Use wine or another acidic liquid to deglaze. Layer your spices — bloom them in oil early, add more later for freshness. Finish with acid, fresh herbs, and a drizzle of good oil. The same principles as any other cooking, just with more deliberate umami building."),

                ("What I wish I'd known before going vegan",
                 "The practical stuff nobody tells you: nutritional yeast becomes a pantry staple immediately, you'll use far more nuts and legumes than you expect, learning to read ingredient labels takes time, eating out is genuinely harder and you should plan ahead. The cooking itself gets easier quickly. The social situations take longer to navigate."),

                ("Roasted cauliflower — the vegetable that converts meat eaters",
                 "Cut into florets, toss generously in olive oil, salt, cumin, and smoked paprika. Roast at 220°C for 30-35 minutes until the edges are deeply caramelized. Serve with tahini sauce (tahini, lemon, garlic, water) and fresh herbs. I've served this at dinner parties where people forgot there was no meat until the end of the meal. The key is heat — high heat, don't crowd the pan."),
            },

            ["World Cuisines"] = new()
            {
                ("The essential pantry for cooking Japanese food at home",
                 "You don't need much to cook most Japanese dishes at home. Core pantry: soy sauce (light and dark), mirin, sake (cooking grade is fine), dashi (instant is acceptable for weeknights), rice vinegar, sesame oil, and short-grain Japanese rice. With these seven ingredients you can make the bases for most Japanese home cooking. Everything else builds from there."),

                ("Why Mexican food is so misunderstood outside Mexico",
                 "What most people think of as Mexican food is Tex-Mex — a regional American cuisine influenced by Mexico. Real Mexican cooking is enormously diverse, regionally specific, and incredibly sophisticated. The moles of Oaxaca, the seafood of Veracruz, the cochinita pibil of the Yucatán — these are completely different culinary traditions that happen to share a country."),

                ("Learning to cook Thai food properly",
                 "The foundation of Thai cooking is building layers: aromatics in the paste (galangal, lemongrass, kaffir lime, shrimp paste), the balance of sweet/sour/salty/spicy, and fish sauce as the salt of choice. The technique that changed my Thai cooking was making paste from scratch — the flavor difference versus jarred is significant. A mortar and pestle is worth buying."),

                ("Moroccan cooking at home — where to start",
                 "Start with the spice blend (ras el hanout) and the technique of slow braising in a tagine or heavy pot. The key flavor combination is sweet and savory: preserved lemon, olives, dried fruit with warm spices and meat. Lamb shoulder slow-cooked with apricots, almonds, and ras el hanout was my entry point and still one of my favorite dishes to cook."),

                ("The regional diversity of Italian cooking",
                 "Italian food is not one cuisine — it's dozens of regional cuisines that happen to share a country. The north (butter, polenta, risotto) is completely different from the south (olive oil, tomatoes, dried pasta). Carbonara is Roman, pesto is Genovese, ragù Bolognese is from Bologna. Understanding the regional origins makes the food make much more sense."),

                ("Korean cooking fundamentals for non-Koreans",
                 "The backbone of Korean flavor is the trinity of fermented condiments: doenjang (fermented soybean paste), gochujang (fermented chili paste), and soy sauce. These are not hot sauces or simple condiments — they're fermented flavor bases that take months to develop. Learn to use these properly and Korean cooking opens up significantly."),

                ("Indian cooking for beginners — starting with the spices",
                 "The intimidating part of Indian cooking is the spice list. The reality is that most recipes use variations of the same core spices: cumin, coriander, turmeric, garam masala, chili. Learn to bloom spices in oil, understand that garam masala goes in near the end (it's a finishing spice), and that good Indian cooking builds flavor in stages. Start with dal — it teaches you everything."),

                ("Ethiopian injera — trying to make it at home",
                 "Injera is a fermented teff flatbread that acts as both plate and utensil in Ethiopian cuisine. Making it at home is an exercise in patience — the teff batter needs 3-5 days to ferment at room temperature. The cooking technique on a flat pan takes practice to get the characteristic spongy holes. I've had mixed success but when it works, it's extraordinary."),

                ("Chinese regional cuisines — they're completely different from each other",
                 "Cantonese (delicate, seafood-focused, dim sum), Sichuan (numbing heat from peppercorns, bold flavors), Shanghainese (sweeter, braised meats), Hunan (spicy, vinegary, less numbing than Sichuan), Xinjiang (lamb, cumin, Central Asian influence) — these are as different from each other as French and Spanish cuisine. When people say 'Chinese food', ask which one."),

                ("Exploring Persian cuisine at home",
                 "Persian food is characterized by the combination of fresh herbs (in enormous quantities — you'd be surprised), fruit with savory dishes, saffron, and the technique of creating a crispy rice crust (tahdig). Start with ghormeh sabzi (herb stew) or fesenjan (walnut and pomegranate chicken). The flavor profiles are unlike any other cuisine and absolutely worth exploring."),

                ("Vietnamese pho — what makes it different from other noodle soups",
                 "The broth is everything. It takes hours to make properly and uses techniques specific to pho: charring the onion and ginger before adding to the stock (essential for color and flavor), toasting whole spices (star anise, cinnamon, cloves, cardamom), and skimming the stock constantly for clarity. The fresh garnishes (herbs, bean sprouts, lime, chili) added at the table are what make it a living dish."),

                ("Spanish cooking beyond paella",
                 "Most people's experience of Spanish food is paella and tapas. The depth of Spanish cuisine is enormous: the egg dishes of Catalonia, the seafood soups of Galicia, the hearty stews of Castile, the sophisticated pintxos of the Basque Country. Fideuà (noodles cooked like paella), cocido madrileño (chickpea stew), and gazpacho are excellent starting points beyond the tourist menu."),
            },

            ["Site Feedback"] = new()
            {
                ("Feature request: being able to save recipes to a personal collection",
                 "I'd love a way to organize saved recipes into custom collections — like a folder system. Right now I just favourite them but finding specific recipes gets harder as the list grows. Something like 'weeknight dinners', 'baking projects', 'to try' would be incredibly useful. Anyone else want this?"),

                ("The forum is great but could use a search function",
                 "I know there was a discussion about cast iron seasoning a while back but I can't find it. A search bar specifically for the forum would be really useful. Even just searching by keywords in post titles would help. Love the community though, great to see it growing."),

                ("Suggestion: add a print-friendly recipe view",
                 "I print recipes to use in the kitchen and the current page format doesn't print well — lots of navigation chrome, ads, and the layout breaks. A clean print view or a print button that generates a simplified version would be really useful. Old fashioned, maybe, but I prefer paper in the kitchen."),

                ("Would love to see a 'recently cooked' feature",
                 "It would be great if you could log when you cooked a recipe and leave a personal note. Not a public review, just a personal cooking journal attached to recipes. Did I like it? What did I change? When did I last make it? This kind of feature would make this site my primary recipe resource."),

                ("Bug report: recipe scaling doesn't work correctly for fractions",
                 "When I try to scale a recipe to 1.5x, ingredients that are listed as fractions (like ½ cup) don't calculate correctly — they show as decimals that are hard to parse. 0.75 cups isn't how anyone measures. Either convert to a useful fraction (¾ cup) or the nearest practical measurement. Small thing but it's annoying every time."),

                ("The mobile experience needs work",
                 "I use this site mostly on my phone while I'm cooking and there are some friction points. The recipe steps are fine but switching between the ingredients and instructions is clunky. A sticky header with tabs for 'ingredients' and 'method' on mobile would be a big improvement. Other than that, really enjoying the site."),

                ("Suggestion: timer functionality built into recipe steps",
                 "Many recipe steps have specific timing ('cook for 5 minutes', 'rest for 30 minutes'). A tap-to-start timer embedded in the recipe step would be so useful while cooking. You'd never need to switch to your phone's clock app. This seems like something achievable and would genuinely improve the cooking experience on the site."),

                ("Love the forum — please don't make it too structured",
                 "Some cooking forums I've been on get overly moderated and categorized to the point where it feels sterile. The conversation here feels natural. Please keep it that way. Fewer rules, more discussion. The quality of conversation comes from the community, not from rigid structure."),
            },
        };

        // Comment templates for realistic threaded conversations
        var commentSets = new List<List<(string Body, List<string> Replies)>>
        {
            new()
            {
                ("This is exactly what I needed to read today. I've been making the same mistake for years.", new() {
                    "Same here! Glad I'm not alone.",
                    "The moment it clicked for me was when I watched a professional cook do it and realized how different the technique was."
                }),
                ("Great post. I'd add that the quality of your ingredients matters more than most people think at this level.", new() {
                    "100% this. You can't make a great dish from bad ingredients.",
                    "Agreed, though technique can compensate more than people realize too."
                }),
            },
            new()
            {
                ("I tried this last week and it was a game changer. My family couldn't believe I made it.", new() {
                    "That's always the best feeling!",
                    "How long did it take you start to finish?"
                }),
                ("Have you tried adding a splash of white wine at this stage? Makes a big difference.", new() {
                    "Good tip, I'll try that next time.",
                    "I prefer a dry sherry personally — a bit more depth."
                }),
            },
            new()
            {
                ("This brought back memories of my grandmother's kitchen. She used to make something very similar.", new() {
                    "There's something special about those family recipes.",
                    "Did she ever write it down or was it all from memory?"
                }),
                ("What temperature do you recommend? I find the timing varies a lot depending on the oven.", new() {
                    "Every oven is different honestly. I always use a thermometer rather than relying on timing.",
                    "Good call. My oven runs hot so I always drop 10 degrees from whatever the recipe says."
                }),
            },
            new()
            {
                ("I've been doing it wrong for years. This explains so much.", new() {
                    "Me too! Why doesn't anyone teach this stuff properly?",
                    "I think it's one of those things that used to be passed down in families and got lost."
                }),
                ("Really appreciate you sharing this. Do you have more tips like this?", new() {
                    "Check the Tips & Techniques section — loads of good stuff there.",
                    "Also some great YouTube channels if you search for it — Jacques Pépin is the master."
                }),
            },
            new()
            {
                ("I made this for dinner last night and it was genuinely one of the best things I've cooked this year.", new() {
                    "High praise! I'm definitely trying it this weekend.",
                    "What did you serve it with?"
                }),
                ("The key step most people rush is the caramelization. Take your time with it.", new() {
                    "This is the lesson that took me longest to learn. Patience is an ingredient.",
                    "Worth setting a timer so you don't convince yourself it's done when it isn't."
                }),
            },
        };

        // ── 4. Generate posts and comments ────────────────────────────────
        var rand    = new Random(42); // fixed seed for reproducibility
        var allPost = new List<ForumPost>();
        int postId  = 1;

        // Build pool of posts from templates
        var postPool = new List<(string FlairName, string Title, string Body)>();

        foreach (var (flairName, posts) in postTemplates)
        {
            foreach (var (title, body) in posts)
            {
                postPool.Add((flairName, title, body));
            }
        }

        // Shuffle and take 300
        postPool = postPool.OrderBy(_ => rand.Next()).ToList();
        var targetPosts = postPool.Take(300).ToList();

        var now = DateTime.UtcNow;

        foreach (var (flairName, title, body) in targetPosts)
        {
            var flair    = flairs.FirstOrDefault(f => f.Name.Contains(flairName.Split(' ')[0]));
            var author   = customers[rand.Next(customers.Count)];
            var daysAgo  = rand.Next(1, 180);
            var hoursAgo = rand.Next(0, 24);
            var postDate = now.AddDays(-daysAgo).AddHours(-hoursAgo);

            var post = new ForumPost
            {
                Title       = title,
                Body        = body,
                CustomerId  = author.Id,
                ForumFlairId = flair?.Id,
                CreatedAt   = postDate,
                IsDeleted   = false,
                Comments    = new List<ForumComment>()
            };

            // Add votes to post
            var voters    = customers.OrderBy(_ => rand.Next()).Take(rand.Next(2, 12)).ToList();
            var postVotes = new List<ForumPostVote>();
            foreach (var voter in voters)
            {
                postVotes.Add(new ForumPostVote
                {
                    CustomerId = voter.Id,
                    Value      = rand.Next(0, 5) > 0 ? 1 : -1
                });
            }
            post.Votes = postVotes;

            // Add comments (2-6 per post)
            var commentSet  = commentSets[rand.Next(commentSets.Count)];
            var numComments = rand.Next(2, 6);

            for (int c = 0; c < Math.Min(numComments, commentSet.Count); c++)
            {
                var (commentBody, replies) = commentSet[c];
                var commenter   = customers[rand.Next(customers.Count)];
                var commentDate = postDate.AddHours(rand.Next(1, 48));

                var comment = new ForumComment
                {
                    Body        = commentBody,
                    CustomerId  = commenter.Id,
                    CreatedAt   = commentDate,
                    IsDeleted   = false,
                    Votes       = new List<ForumCommentVote>(),
                    Replies     = new List<ForumComment>()
                };

                // Comment votes
                var commentVoters = customers.OrderBy(_ => rand.Next()).Take(rand.Next(1, 6)).ToList();
                foreach (var voter in commentVoters)
                {
                    comment.Votes.Add(new ForumCommentVote
                    {
                        CustomerId = voter.Id,
                        Value      = rand.Next(0, 4) > 0 ? 1 : -1
                    });
                }

                // Add replies
                if (replies.Any())
                {
                    int replyCount = rand.Next(1, replies.Count + 1);
                    for (int r = 0; r < replyCount; r++)
                    {
                        var replier   = customers[rand.Next(customers.Count)];
                        var replyDate = commentDate.AddHours(rand.Next(1, 24));

                        comment.Replies.Add(new ForumComment
                        {
                            Body        = replies[r],
                            CustomerId  = replier.Id,
                            CreatedAt   = replyDate,
                            IsDeleted   = false,
                            Votes       = new List<ForumCommentVote>(),
                            Replies     = new List<ForumComment>()
                        });
                    }
                }

                post.Comments.Add(comment);
            }

            allPost.Add(post);
        }

        await db.ForumPosts.AddRangeAsync(allPost);
        await db.SaveChangesAsync();

        Console.WriteLine($"✅ Forum seeded: {allPost.Count} posts across {flairs.Count} flairs with {customers.Count} community members.");
    }
}