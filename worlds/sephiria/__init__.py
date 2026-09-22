from BaseClasses import Region, ItemClassification
from worlds.AutoWorld import World, WebWorld

from .Options import SephiriaOptions

from .Items import SephiriaItem, item_table
from .Locations import SephiriaLocation, location_table


class SephiriaWebWorld(WebWorld):
    pass


class SephiriaWorld(World):
    game = "Sephiria"
    web = SephiriaWebWorld()

    options_dataclass = SephiriaOptions

    item_name_to_id = item_table
    location_name_to_id = location_table

    def create_regions(self):
        menu = Region("Menu", self.player, self.multiworld)

        test_location = SephiriaLocation(
            self.player,
            "Test Location",
            location_table["Test Location"],
            menu
        )

        menu.locations.append(test_location)
        self.multiworld.regions.append(menu)

    def create_items(self):
        item = SephiriaItem(
            "Test Item",
            ItemClassification.filler,
            item_table["Test Item"],
            self.player
        )

        self.multiworld.itempool.append(item)

    def fill_slot_data(self):
        return {
            "goal_chapter": self.options.goal_chapter.value,
            "required_chapter_clears": self.options.required_chapter_clears.value,
            "unique_weapons_required": bool(
                self.options.unique_weapons_required.value    
            ),
        }
