using System;
using System.Collections.Generic;
namespace SimpleColony
{
    // Shared content for object cards and the hierarchical in-game reference.
    public static class WikiCatalog
    {
        static readonly List<WikiArticle> articles = new List<WikiArticle>();
        static readonly Dictionary<string,WikiArticle> index = new Dictionary<string,WikiArticle>();
        public static IReadOnlyList<WikiArticle> All { get; }
        public static string ResourceId(Resource resource) => "resource/"+resource;
        public static string ProfessionId(Profession profession) => "profession/"+profession;
        public static string BuildingId(BuildingType building) => "building/"+building;
        public static string ResearchId(ResearchType research) => "research/"+research;
        public static WikiArticle Get(string id) => index[id];
        public static IEnumerable<WikiArticle> Children(string id)
        {
            foreach(var article in articles) if(article.ParentId==id)yield return article;
        }
        static void Add(string id,string parent,string title,string body,params string[] related)
        {
            var article=new WikiArticle(id,parent,title,body,related);
            index.Add(id,article); articles.Add(article);
        }
        static WikiCatalog()
        {
            Add("root",null,"Справочник","Выберите раздел. Статьи связаны с информацией об объектах на карте.");
            Add("objects","root","Объекты мира","Что находится на карте и как читать обозначения.");
            Add("resources","objects","Ресурсы","Цветные секторы обводки — независимые запасы. Полный сектор означает полный запас, серый — исчерпанный. Нажмите сектор, затем кнопку информации.","objects/node","mechanics/logistics");
            string[] resourceNames={"Питание","Знания","Материя","Заряд","Лекарства","Эссенция"};
            string[] resourceBodies={
                "Собиратели добывают питание и доставляют в ядро. Там пешки едят. Питание расходуется также на переходы, найм и строительство. Источники питания и фермы восстанавливают запас.",
                "Исследователь работает у синего запаса и получает знания непосредственно в общий склад. Знания покупают улучшения колонии. Для профессии и покупок нужен модуль исследований.",
                "Строитель доставляет материю со специальных территорий и мастерских. Материя расходуется при заказе постройки. Автоматического списания на транспорт нет.",
                "Снабженец доставляет заряд в ядро. Из общего склада заряд автоматически расходуется на восстановление истощённого пищевого источника.",
                "Медик доставляет лекарства в ядро. Вернувшиеся раненые пешки расходуют их для восстановления здоровья.",
                "Снабженец собирает эссенцию и приносит в ядро. Стабилизатор берёт запас, идёт на территорию и создаёт защиту. Одна единица эссенции даёт 3 защиты, максимум 30 на узел."
            };
            int[] gatherers={0,3,4,5,6,5};
            for(int i=0;i<6;i++) Add(ResourceId((Resource)i),"resources",resourceNames[i],resourceBodies[i],"mechanics/logistics",ProfessionId((Profession)gatherers[i]),i==5?"mechanics/fortification":i==1?"mechanics/research":"resources");
            Add("objects/pawn","objects","Пешки","Кружок — пешка, цвет — профессия. Квадрат рядом — груз. В нижней панели видны здоровье, сытость, силы и задача. Смена профессии выполняется на ядре.","professions","mechanics/orders","mechanics/needs");
            Add("professions","objects/pawn","Профессии","Обязанности пешек задаются стратегиями. Создайте безработного в управлении колонией и назначьте профессию.","mechanics/workforce");
            string[] names={"Собиратель","Защитник","Разведчик","Исследователь","Строитель","Снабженец","Медик","Стабилизатор","Безработный"};
            string[] bodies={
                "Добывает питание и переносит груз в ядро. Для работы нужны безопасная территория и запас на поход и возвращение.",
                "Подавляет очаги заражения. Есть три режима — поиск угроз, патруль и пост. Возвращается за едой и отдыхом. Режим и пост сохраняются после снабжения.",
                "Идёт к доступной границе известной карты и обследует территорию. Открытие расширяет граф и добавляет новые узлы. Дальность ограничена запасами для дороги.",
                "Остаётся у местного запаса знаний и преобразует его в общий ресурс. Груз домой не переносит. Требует включённого модуля исследований.",
                "Доставляет материю. Если есть доступный заказ строительства, выполняет работу на территории. Заказ оплачивается со склада один раз.",
                "Собирает заряд и эссенцию, переносит их в ядро. Старается пополнять менее обеспеченный склад из доступных ресурсов территории.",
                "Добывает лекарства и доставляет их в ядро. Лечение раненых происходит на базе при наличии лекарств.",
                "Загружает эссенцию на ядре, сам выбирает безопасный узел и укрепляет его. Предпочитает территории рядом с угрозами. После расходования груза возвращается за новой порцией.",
                "Ожидает назначения. Создаётся в меню пешек за 25 питания. Кнопки +1 и +3 распределяют свободных работников, −1 освобождает работника."
            };
            string[] specialties={"mechanics/logistics","mechanics/defense","mechanics/world","mechanics/research","mechanics/construction","mechanics/logistics","mechanics/needs","mechanics/fortification","mechanics/workforce"};
            for(int i=0;i<names.Length;i++) Add(ProfessionId((Profession)i),"professions",names[i],bodies[i],"mechanics/workforce","mechanics/needs",specialties[i]);
            Add("objects/core","objects","Ядро","Центр снабжения. Пешки сдают груз, едят, восстанавливают силы и меняют назначенную профессию. Лекарства расходуются на лечение. Все складские ресурсы показаны наверху.","mechanics/workforce","resources","mechanics/needs");
            Add("objects/node","objects","Территория","Содержит несколько запасов либо остаётся пустой. Запасы показаны секторами обводки. У открытого безопасного узла можно заказать постройку. Закрытие доступа отправляет находящихся на нём и прибывающих пешек в ядро.","resources","mechanics/construction","mechanics/access","mechanics/fortification");
            Add("objects/road","objects","Дороги","Переход расходует питание и силы. Зелёный цвет означает меньший расход еды, красный — больший. До назначения работы учитываются дорога к цели и возвращение. Путевой пост снижает расход соседних переходов на 30%.","mechanics/logistics",BuildingId(BuildingType.Waystation),"research/Transport","research/Rations");
            Add("objects/threat","objects","Очаг заражения","Красный значок с восклицанием — угроза. Очаг уничтожает местные запасы и распространяется по дорогам. Новые очаги возникают на максимально далёких от ядра узлах по длине пути. Защита территории поглощает входящее заражение.","mechanics/defense","mechanics/fortification",ProfessionId(Profession.Defender));
            Add("mechanics","root","Механики","Как устроены экономика, работы и изменение мира.");
            Add("mechanics/needs","mechanics","Здоровье, сытость и силы","Голод уменьшает здоровье, дороги расходуют еду и силы. Пешка учитывает запас для возвращения и отправляется в ядро для снабжения. В ядре силы восстанавливаются, еда пополняется со склада, лекарства лечат ранения.",ResourceId(Resource.Food),ResourceId(Resource.Medicine),"objects/core");
            Add("mechanics/logistics","mechanics","Добыча и доставка","Пешка выбирает достижимую работу, идёт к ресурсу, добывает груз и возвращается. Груз сохраняет собственный тип при смене профессии. Исследователь получает знания на месте. Стабилизатор использует эссенцию на целевом узле.","objects/road","resources","professions");
            Add("mechanics/workforce","mechanics","Создание и назначения","В меню пешек создаётся серый безработный за 25 питания. +1 и +3 назначают свободных работников, −1 освобождает работника. У отдельной пешки кнопка смены работы циклически назначает следующую профессию. Перевод завершается на ядре.","professions","objects/core");
            Add("mechanics/orders","mechanics","Приказ движения","Выберите пешку, нажмите значок направления справа и открытую клетку. Проверяются путь и запасы для возвращения. Пешка удерживает место до отмены приказа или снабжения. Повторное нажатие отменяет приказ. У воина в режиме поста направление задаёт место поста.","mechanics/defense","objects/road");
            Add("mechanics/access","mechanics","Доступ к территории","Закрытая клетка недоступна для обычных работ. Находящиеся на ней и прибывающие пешки эвакуируются в ядро. Приказ удерживать закрытый узел отменяется. Открыть доступ можно той же кнопкой замка.","objects/node","mechanics/orders");
            Add("mechanics/defense","mechanics","Работа защитников","Переключатель режима находится справа у выбранного воина. Текущий режим виден внизу. Воин сохраняет запас на возвращение.","defense/Pursuit","defense/Patrol","defense/Guard","objects/threat");
            Add("defense/Pursuit","mechanics/defense","Поиск угроз","Воин самостоятельно выбирает доступные очаги, включая дальние земли. После победы ищет следующую цель. Недостаток запасов требует возвращения на ядро.","objects/threat","mechanics/needs");
            Add("defense/Patrol","mechanics/defense","Патруль","Воин обходит доступные граничные территории по кругу и реагирует на очаги в известной области и у её границы. Если запаса не хватает, возвращается для снабжения.","mechanics/orders","objects/road");
            Add("defense/Guard","mechanics/defense","Пост","Воин удерживает указанную клетку и подавляет заражение на ней. Для назначения места используется кнопка направления. После еды и отдыха возвращается на тот же пост. Смена режима снимает назначение.","mechanics/orders","mechanics/access");
            Add("mechanics/fortification","mechanics","Укрепление территории","Стабилизатор расходует привезённую эссенцию и создаёт до 30 защиты. Она сохраняется до нападения. Входящее заражение сначала уменьшает защиту, и лишь остаток становится очагом. Дополнительное фиолетовое кольцо показывает запас защиты.",ResourceId(Resource.Stability),ProfessionId(Profession.Supplier),ProfessionId(Profession.Stabilizer));
            Add("mechanics/construction","mechanics","Строительство","Выберите открытую безопасную клетку и значок стройки справа. Материалы списываются один раз при заказе. Строитель выполняет 15 игровых секунд работы на месте. На территории помещается одна постройка.","buildings",ProfessionId(Profession.Builder),ResourceId(Resource.Matter));
            Add("buildings","mechanics/construction","Постройки","Выберите тип постройки.");
            foreach(var d in BuildingDefinition.All) Add(BuildingId(d.Type),"buildings",d.Name,d.Description+"\n\nЦена — "+d.FoodCost+" еды и "+d.MatterCost+" материи. Строитель выполняет 15 секунд работы.","mechanics/construction");
            Add("mechanics/research","mechanics","Исследования","Исследователь добывает знания на территориях. Покупки доступны в меню исследований, каждое улучшение имеет три уровня. Цена очередного уровня равна базовой цене, умноженной на номер уровня.",ResourceId(Resource.Knowledge),ProfessionId(Profession.Researcher));
            foreach(var d in ResearchProgress.Definitions) Add(ResearchId(d.Type),"mechanics/research",d.Name,d.Description+"\n\nБазовая цена — "+d.Cost+" знаний. Всего 3 уровня.","mechanics/research");
            Add("mechanics/world","mechanics","Генерация мира","Новая игра получает seed от времени. Разведка достраивает карту. Большинство новых территорий пустые. Ресурсы перемешиваются блоками, около ядра гарантированы питание, материя и знания. Дальность освоения ограничивается снабжением.","objects/node",ProfessionId(Profession.Scout),"resources");
            Add("mechanics/modules","mechanics","Модули","Угрозы и исследования подключаются при создании сеанса. Отключённая механика не создаёт заданий своих профессий. Состав можно задать полем Session Mechanics перед перезапуском карты в редакторе.","mechanics/research","mechanics/defense");
            Add("controls","root","Управление","Работа с картой, временем и информацией.");
            Add("controls/map","controls","Камера и выбор","Перетаскивание пальцем или мышью двигает карту. Два пальца или колесо меняют масштаб. Касание выбирает объект, его данные видны внизу. Действия находятся справа. Подробная карточка открывается кнопкой информации, удержание значка объясняет действие.","objects/pawn","resources","objects/road");
            Add("controls/time","controls","Пауза и скорость","Пауза останавливает симуляцию. − и + переключают скорости от ×0,25 до MAX. MAX использует доступный бюджет процессора. Перезапуск создаёт новую карту и сбрасывает прогресс.","controls/debug");
            Add("controls/debug","controls","Диагностика и DEV","Диагностика показывает seed, время, размер мира, FPS, TPS и стоимость тика. DEV +1000 каждого добавляет по 1000 всех шести ресурсов. Это инструмент проверки прототипа.","controls/time","resources");
            foreach(var article in articles)
            {
                if(article.ParentId!=null && !index.ContainsKey(article.ParentId))throw new InvalidOperationException(article.Id);
                foreach(var link in article.Related)if(!index.ContainsKey(link))throw new InvalidOperationException("Missing wiki article "+link);
            }
            All=articles.AsReadOnly();
        }
    }
}
