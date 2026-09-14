from dataclasses import dataclass

from Options import Choice, Range, Toggle, PerGameCommonOptions


class GoalChapter(Choice):
    """
    The chapter that must be completed to finish the Archipelago goal
    """

    display_name = "Goal Chapter"

    option_1 = 1
    option_2 = 2
    option_3 = 3
    option_4 = 4
    option_5 = 5
    option_6 = 6

    default = 1


class RequiredChapterClears(Range):
    """
    The number of times the selected goal chapter must be completed.
    """

    display_name = "Required Chapter Clears"

    range_start = 1
    range_end = 6
    default = 1


class UniqueWeaponsRequired(Toggle):
    """
    If enabled, each required chapter clear must use a different weapon.
    """

    display_name = "Require Unique Weapons"


@dataclass
class SephiriaOptions(PerGameCommonOptions):
    goal_chapter: GoalChapter
    required_chapter_clears: RequiredChapterClears
    unique_weapons_required: UniqueWeaponsRequired