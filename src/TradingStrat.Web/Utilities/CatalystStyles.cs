namespace TradingStrat.Web.Utilities;

/// <summary>
/// Centralized Catalyst UI Kit style constants for consistent component styling.
/// Complements TailwindStyles.cs with Catalyst-specific patterns.
/// </summary>
public static class CatalystStyles
{
    // ========== Button Color Variants ==========

    /// <summary>
    /// Blue button (primary action color)
    /// </summary>
    public const string ButtonBlue = "[--btn-bg:theme(colors.blue.600)] [--btn-border:theme(colors.blue.700)] text-white [--btn-icon:theme(colors.blue.400)]";

    /// <summary>
    /// Red button (danger/destructive actions)
    /// </summary>
    public const string ButtonRed = "[--btn-bg:theme(colors.red.600)] [--btn-border:theme(colors.red.700)] text-white [--btn-icon:theme(colors.red.300)]";

    /// <summary>
    /// Green button (success/confirmation)
    /// </summary>
    public const string ButtonGreen = "[--btn-bg:theme(colors.green.600)] [--btn-border:theme(colors.green.700)] text-white [--btn-icon:rgb(255_255_255/0.6)]";

    /// <summary>
    /// Orange button (warning)
    /// </summary>
    public const string ButtonOrange = "[--btn-bg:theme(colors.orange.500)] [--btn-border:theme(colors.orange.600)] text-white [--btn-icon:theme(colors.orange.300)]";

    /// <summary>
    /// Indigo button
    /// </summary>
    public const string ButtonIndigo = "[--btn-bg:theme(colors.indigo.500)] [--btn-border:theme(colors.indigo.600)] text-white [--btn-icon:theme(colors.indigo.300)]";

    /// <summary>
    /// Cyan button
    /// </summary>
    public const string ButtonCyan = "[--btn-bg:theme(colors.cyan.300)] [--btn-border:theme(colors.cyan.400)] text-cyan-950 [--btn-icon:theme(colors.cyan.500)]";

    /// <summary>
    /// Amber button
    /// </summary>
    public const string ButtonAmber = "[--btn-bg:theme(colors.amber.400)] [--btn-border:theme(colors.amber.500)] text-amber-950 [--btn-icon:theme(colors.amber.600)]";

    /// <summary>
    /// Yellow button
    /// </summary>
    public const string ButtonYellow = "[--btn-bg:theme(colors.yellow.300)] [--btn-border:theme(colors.yellow.400)] text-yellow-950 [--btn-icon:theme(colors.yellow.600)]";

    /// <summary>
    /// Lime button
    /// </summary>
    public const string ButtonLime = "[--btn-bg:theme(colors.lime.300)] [--btn-border:theme(colors.lime.400)] text-lime-950 [--btn-icon:theme(colors.lime.600)]";

    /// <summary>
    /// Emerald button
    /// </summary>
    public const string ButtonEmerald = "[--btn-bg:theme(colors.emerald.600)] [--btn-border:theme(colors.emerald.700)] text-white [--btn-icon:rgb(255_255_255/0.6)]";

    /// <summary>
    /// Teal button
    /// </summary>
    public const string ButtonTeal = "[--btn-bg:theme(colors.teal.600)] [--btn-border:theme(colors.teal.700)] text-white [--btn-icon:rgb(255_255_255/0.6)]";

    /// <summary>
    /// Sky button
    /// </summary>
    public const string ButtonSky = "[--btn-bg:theme(colors.sky.500)] [--btn-border:theme(colors.sky.600)] text-white [--btn-icon:rgb(255_255_255/0.6)]";

    /// <summary>
    /// Violet button
    /// </summary>
    public const string ButtonViolet = "[--btn-bg:theme(colors.violet.500)] [--btn-border:theme(colors.violet.600)] text-white [--btn-icon:theme(colors.violet.300)]";

    /// <summary>
    /// Purple button
    /// </summary>
    public const string ButtonPurple = "[--btn-bg:theme(colors.purple.500)] [--btn-border:theme(colors.purple.600)] text-white [--btn-icon:theme(colors.purple.300)]";

    /// <summary>
    /// Fuchsia button
    /// </summary>
    public const string ButtonFuchsia = "[--btn-bg:theme(colors.fuchsia.500)] [--btn-border:theme(colors.fuchsia.600)] text-white [--btn-icon:theme(colors.fuchsia.300)]";

    /// <summary>
    /// Pink button
    /// </summary>
    public const string ButtonPink = "[--btn-bg:theme(colors.pink.500)] [--btn-border:theme(colors.pink.600)] text-white [--btn-icon:theme(colors.pink.300)]";

    /// <summary>
    /// Rose button
    /// </summary>
    public const string ButtonRose = "[--btn-bg:theme(colors.rose.500)] [--btn-border:theme(colors.rose.600)] text-white [--btn-icon:theme(colors.rose.300)]";

    /// <summary>
    /// Zinc button (default neutral)
    /// </summary>
    public const string ButtonZinc = "[--btn-bg:theme(colors.zinc.600)] [--btn-border:theme(colors.zinc.700)] text-white [--btn-icon:theme(colors.zinc.400)]";

    /// <summary>
    /// Dark/Zinc button (default button style)
    /// </summary>
    public const string ButtonDarkZinc = "[--btn-bg:theme(colors.zinc.900)] [--btn-border:theme(colors.zinc.950)] text-white [--btn-icon:theme(colors.zinc.400)] dark:[--btn-bg:theme(colors.zinc.600)]";

    /// <summary>
    /// White button
    /// </summary>
    public const string ButtonWhite = "[--btn-bg:white] [--btn-border:rgb(9_9_11/0.1)] text-zinc-950 [--btn-icon:theme(colors.zinc.400)]";

    /// <summary>
    /// Dark button
    /// </summary>
    public const string ButtonDark = "[--btn-bg:theme(colors.zinc.900)] [--btn-border:theme(colors.zinc.950)] text-white [--btn-icon:theme(colors.zinc.400)]";

    /// <summary>
    /// Light button
    /// </summary>
    public const string ButtonLight = "[--btn-bg:white] [--btn-border:rgb(9_9_11/0.1)] text-zinc-950 [--btn-icon:theme(colors.zinc.500)] dark:[--btn-bg:theme(colors.zinc.800)]";

    // ========== Dialog Sizes ==========

    /// <summary>
    /// Extra small dialog (xs)
    /// </summary>
    public const string DialogSizeXS = "sm:max-w-xs";

    /// <summary>
    /// Small dialog (sm)
    /// </summary>
    public const string DialogSizeSmall = "sm:max-w-sm";

    /// <summary>
    /// Medium dialog (md) - default
    /// </summary>
    public const string DialogSizeMedium = "sm:max-w-md";

    /// <summary>
    /// Large dialog (lg)
    /// </summary>
    public const string DialogSizeLarge = "sm:max-w-lg";

    /// <summary>
    /// Extra large dialog (xl)
    /// </summary>
    public const string DialogSizeXL = "sm:max-w-xl";

    /// <summary>
    /// 2XL dialog
    /// </summary>
    public const string DialogSize2XL = "sm:max-w-2xl";

    /// <summary>
    /// 3XL dialog
    /// </summary>
    public const string DialogSize3XL = "sm:max-w-3xl";

    /// <summary>
    /// 4XL dialog
    /// </summary>
    public const string DialogSize4XL = "sm:max-w-4xl";

    /// <summary>
    /// 5XL dialog
    /// </summary>
    public const string DialogSize5XL = "sm:max-w-5xl";

    // ========== Table Modes ==========

    /// <summary>
    /// Striped table rows (alternating background colors)
    /// </summary>
    public const string TableStriped = "even:bg-gray-50 dark:even:bg-gray-800";

    /// <summary>
    /// Dense table (reduced padding)
    /// </summary>
    public const string TableDense = "py-2";

    /// <summary>
    /// Grid table (visible borders)
    /// </summary>
    public const string TableGrid = "border-collapse border border-gray-200 dark:border-dark-border";

    /// <summary>
    /// Bleed table (extends to container edges)
    /// </summary>
    public const string TableBleed = "-mx-6";

    // ========== Badge Colors ==========

    /// <summary>
    /// Blue badge
    /// </summary>
    public const string BadgeBlue = "bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300";

    /// <summary>
    /// Red badge
    /// </summary>
    public const string BadgeRed = "bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300";

    /// <summary>
    /// Green badge
    /// </summary>
    public const string BadgeGreen = "bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300";

    /// <summary>
    /// Yellow badge
    /// </summary>
    public const string BadgeYellow = "bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300";

    /// <summary>
    /// Gray badge
    /// </summary>
    public const string BadgeGray = "bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-300";

    // ========== Input States ==========

    /// <summary>
    /// Invalid input state (error)
    /// </summary>
    public const string InputInvalid = "border-red-500 dark:border-red-600 focus:border-red-500 dark:focus:border-red-600 focus:ring-red-500 dark:focus:ring-red-600";

    /// <summary>
    /// Valid input state (success)
    /// </summary>
    public const string InputValid = "border-green-500 dark:border-green-600 focus:border-green-500 dark:focus:border-green-600 focus:ring-green-500 dark:focus:ring-green-600";

    // ========== Touch Target (Accessibility) ==========

    /// <summary>
    /// Expands hit area to at least 44x44px on touch devices (WCAG compliance)
    /// </summary>
    public const string TouchTarget = "absolute top-1/2 left-1/2 size-[max(100%,2.75rem)] -translate-x-1/2 -translate-y-1/2 [@media(pointer:fine)]:hidden";

    // ========== Checkbox Colors (Catalyst UI Kit) ==========

    /// <summary>
    /// Dark/Zinc checkbox (default)
    /// </summary>
    public const string CheckboxDarkZinc = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-zinc-900)] [--checkbox-checked-border:var(--color-zinc-950)]/90 dark:[--checkbox-checked-bg:var(--color-zinc-600)]";

    /// <summary>
    /// Dark/White checkbox
    /// </summary>
    public const string CheckboxDarkWhite = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-zinc-900)] [--checkbox-checked-border:var(--color-zinc-950)]/90 dark:[--checkbox-check:var(--color-zinc-900)] dark:[--checkbox-checked-bg:var(--color-white)] dark:[--checkbox-checked-border:var(--color-zinc-950)]/15";

    /// <summary>
    /// White checkbox
    /// </summary>
    public const string CheckboxWhite = "[--checkbox-check:var(--color-zinc-900)] [--checkbox-checked-bg:var(--color-white)] [--checkbox-checked-border:var(--color-zinc-950)]/15";

    /// <summary>
    /// Dark checkbox
    /// </summary>
    public const string CheckboxDark = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-zinc-900)] [--checkbox-checked-border:var(--color-zinc-950)]/90";

    /// <summary>
    /// Zinc checkbox
    /// </summary>
    public const string CheckboxZinc = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-zinc-600)] [--checkbox-checked-border:var(--color-zinc-700)]/90";

    /// <summary>
    /// Blue checkbox
    /// </summary>
    public const string CheckboxBlue = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-blue-600)] [--checkbox-checked-border:var(--color-blue-700)]/90";

    /// <summary>
    /// Red checkbox
    /// </summary>
    public const string CheckboxRed = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-red-600)] [--checkbox-checked-border:var(--color-red-700)]/90";

    /// <summary>
    /// Green checkbox
    /// </summary>
    public const string CheckboxGreen = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-green-600)] [--checkbox-checked-border:var(--color-green-700)]/90";

    /// <summary>
    /// Orange checkbox
    /// </summary>
    public const string CheckboxOrange = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-orange-500)] [--checkbox-checked-border:var(--color-orange-600)]/90";

    /// <summary>
    /// Amber checkbox
    /// </summary>
    public const string CheckboxAmber = "[--checkbox-check:var(--color-amber-950)] [--checkbox-checked-bg:var(--color-amber-400)] [--checkbox-checked-border:var(--color-amber-500)]/80";

    /// <summary>
    /// Yellow checkbox
    /// </summary>
    public const string CheckboxYellow = "[--checkbox-check:var(--color-yellow-950)] [--checkbox-checked-bg:var(--color-yellow-300)] [--checkbox-checked-border:var(--color-yellow-400)]/80";

    /// <summary>
    /// Lime checkbox
    /// </summary>
    public const string CheckboxLime = "[--checkbox-check:var(--color-lime-950)] [--checkbox-checked-bg:var(--color-lime-300)] [--checkbox-checked-border:var(--color-lime-400)]/80";

    /// <summary>
    /// Emerald checkbox
    /// </summary>
    public const string CheckboxEmerald = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-emerald-600)] [--checkbox-checked-border:var(--color-emerald-700)]/90";

    /// <summary>
    /// Teal checkbox
    /// </summary>
    public const string CheckboxTeal = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-teal-600)] [--checkbox-checked-border:var(--color-teal-700)]/90";

    /// <summary>
    /// Cyan checkbox
    /// </summary>
    public const string CheckboxCyan = "[--checkbox-check:var(--color-cyan-950)] [--checkbox-checked-bg:var(--color-cyan-300)] [--checkbox-checked-border:var(--color-cyan-400)]/80";

    /// <summary>
    /// Sky checkbox
    /// </summary>
    public const string CheckboxSky = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-sky-500)] [--checkbox-checked-border:var(--color-sky-600)]/80";

    /// <summary>
    /// Indigo checkbox
    /// </summary>
    public const string CheckboxIndigo = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-indigo-500)] [--checkbox-checked-border:var(--color-indigo-600)]/90";

    /// <summary>
    /// Violet checkbox
    /// </summary>
    public const string CheckboxViolet = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-violet-500)] [--checkbox-checked-border:var(--color-violet-600)]/90";

    /// <summary>
    /// Purple checkbox
    /// </summary>
    public const string CheckboxPurple = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-purple-500)] [--checkbox-checked-border:var(--color-purple-600)]/90";

    /// <summary>
    /// Fuchsia checkbox
    /// </summary>
    public const string CheckboxFuchsia = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-fuchsia-500)] [--checkbox-checked-border:var(--color-fuchsia-600)]/90";

    /// <summary>
    /// Pink checkbox
    /// </summary>
    public const string CheckboxPink = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-pink-500)] [--checkbox-checked-border:var(--color-pink-600)]/90";

    /// <summary>
    /// Rose checkbox
    /// </summary>
    public const string CheckboxRose = "[--checkbox-check:var(--color-white)] [--checkbox-checked-bg:var(--color-rose-500)] [--checkbox-checked-border:var(--color-rose-600)]/90";

    // ========== Radio Colors (Catalyst UI Kit) ==========

    /// <summary>
    /// Dark/Zinc radio (default)
    /// </summary>
    public const string RadioDarkZinc = "[--radio-checked-bg:var(--color-zinc-900)] [--radio-checked-border:var(--color-zinc-950)]/90 [--radio-checked-indicator:var(--color-white)] dark:[--radio-checked-bg:var(--color-zinc-600)]";

    /// <summary>
    /// Dark/White radio
    /// </summary>
    public const string RadioDarkWhite = "[--radio-checked-bg:var(--color-zinc-900)] [--radio-checked-border:var(--color-zinc-950)]/90 [--radio-checked-indicator:var(--color-white)] dark:[--radio-checked-bg:var(--color-white)] dark:[--radio-checked-border:var(--color-zinc-950)]/15 dark:[--radio-checked-indicator:var(--color-zinc-900)]";

    /// <summary>
    /// White radio
    /// </summary>
    public const string RadioWhite = "[--radio-checked-bg:var(--color-white)] [--radio-checked-border:var(--color-zinc-950)]/15 [--radio-checked-indicator:var(--color-zinc-900)]";

    /// <summary>
    /// Dark radio
    /// </summary>
    public const string RadioDark = "[--radio-checked-bg:var(--color-zinc-900)] [--radio-checked-border:var(--color-zinc-950)]/90 [--radio-checked-indicator:var(--color-white)]";

    /// <summary>
    /// Zinc radio
    /// </summary>
    public const string RadioZinc = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-zinc-600)] [--radio-checked-border:var(--color-zinc-700)]/90";

    /// <summary>
    /// Blue radio
    /// </summary>
    public const string RadioBlue = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-blue-600)] [--radio-checked-border:var(--color-blue-700)]/90";

    /// <summary>
    /// Red radio
    /// </summary>
    public const string RadioRed = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-red-600)] [--radio-checked-border:var(--color-red-700)]/90";

    /// <summary>
    /// Green radio
    /// </summary>
    public const string RadioGreen = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-green-600)] [--radio-checked-border:var(--color-green-700)]/90";

    /// <summary>
    /// Orange radio
    /// </summary>
    public const string RadioOrange = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-orange-500)] [--radio-checked-border:var(--color-orange-600)]/90";

    /// <summary>
    /// Amber radio
    /// </summary>
    public const string RadioAmber = "[--radio-checked-bg:var(--color-amber-400)] [--radio-checked-border:var(--color-amber-500)]/80 [--radio-checked-indicator:var(--color-amber-950)]";

    /// <summary>
    /// Yellow radio
    /// </summary>
    public const string RadioYellow = "[--radio-checked-bg:var(--color-yellow-300)] [--radio-checked-border:var(--color-yellow-400)]/80 [--radio-checked-indicator:var(--color-yellow-950)]";

    /// <summary>
    /// Lime radio
    /// </summary>
    public const string RadioLime = "[--radio-checked-bg:var(--color-lime-300)] [--radio-checked-border:var(--color-lime-400)]/80 [--radio-checked-indicator:var(--color-lime-950)]";

    /// <summary>
    /// Emerald radio
    /// </summary>
    public const string RadioEmerald = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-emerald-600)] [--radio-checked-border:var(--color-emerald-700)]/90";

    /// <summary>
    /// Teal radio
    /// </summary>
    public const string RadioTeal = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-teal-600)] [--radio-checked-border:var(--color-teal-700)]/90";

    /// <summary>
    /// Cyan radio
    /// </summary>
    public const string RadioCyan = "[--radio-checked-bg:var(--color-cyan-300)] [--radio-checked-border:var(--color-cyan-400)]/80 [--radio-checked-indicator:var(--color-cyan-950)]";

    /// <summary>
    /// Sky radio
    /// </summary>
    public const string RadioSky = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-sky-500)] [--radio-checked-border:var(--color-sky-600)]/80";

    /// <summary>
    /// Indigo radio
    /// </summary>
    public const string RadioIndigo = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-indigo-500)] [--radio-checked-border:var(--color-indigo-600)]/90";

    /// <summary>
    /// Violet radio
    /// </summary>
    public const string RadioViolet = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-violet-500)] [--radio-checked-border:var(--color-violet-600)]/90";

    /// <summary>
    /// Purple radio
    /// </summary>
    public const string RadioPurple = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-purple-500)] [--radio-checked-border:var(--color-purple-600)]/90";

    /// <summary>
    /// Fuchsia radio
    /// </summary>
    public const string RadioFuchsia = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-fuchsia-500)] [--radio-checked-border:var(--color-fuchsia-600)]/90";

    /// <summary>
    /// Pink radio
    /// </summary>
    public const string RadioPink = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-pink-500)] [--radio-checked-border:var(--color-pink-600)]/90";

    /// <summary>
    /// Rose radio
    /// </summary>
    public const string RadioRose = "[--radio-checked-indicator:var(--color-white)] [--radio-checked-bg:var(--color-rose-500)] [--radio-checked-border:var(--color-rose-600)]/90";

    // ========== Switch Colors (Catalyst UI Kit) ==========

    /// <summary>
    /// Dark/Zinc switch (default)
    /// </summary>
    public const string SwitchDarkZinc = "[--switch-bg-ring:var(--color-zinc-950)]/90 [--switch-bg:var(--color-zinc-900)] dark:[--switch-bg-ring:transparent] dark:[--switch-bg:var(--color-white)]/25 [--switch-ring:var(--color-zinc-950)]/90 [--switch-shadow:var(--color-black)]/10 [--switch:white] dark:[--switch-ring:var(--color-zinc-700)]/90";

    /// <summary>
    /// Dark/White switch
    /// </summary>
    public const string SwitchDarkWhite = "[--switch-bg-ring:var(--color-zinc-950)]/90 [--switch-bg:var(--color-zinc-900)] dark:[--switch-bg-ring:transparent] dark:[--switch-bg:var(--color-white)] [--switch-ring:var(--color-zinc-950)]/90 [--switch-shadow:var(--color-black)]/10 [--switch:white] dark:[--switch-ring:transparent] dark:[--switch:var(--color-zinc-900)]";

    /// <summary>
    /// White switch
    /// </summary>
    public const string SwitchWhite = "[--switch-bg-ring:var(--color-black)]/15 [--switch-bg:white] dark:[--switch-bg-ring:transparent] [--switch-shadow:var(--color-black)]/10 [--switch-ring:transparent] [--switch:var(--color-zinc-950)]";

    /// <summary>
    /// Dark switch
    /// </summary>
    public const string SwitchDark = "[--switch-bg-ring:var(--color-zinc-950)]/90 [--switch-bg:var(--color-zinc-900)] dark:[--switch-bg-ring:var(--color-white)]/15 [--switch-ring:var(--color-zinc-950)]/90 [--switch-shadow:var(--color-black)]/10 [--switch:white]";

    /// <summary>
    /// Zinc switch
    /// </summary>
    public const string SwitchZinc = "[--switch-bg-ring:var(--color-zinc-700)]/90 [--switch-bg:var(--color-zinc-600)] dark:[--switch-bg-ring:transparent] [--switch-shadow:var(--color-black)]/10 [--switch:white] [--switch-ring:var(--color-zinc-700)]/90";

    /// <summary>
    /// Blue switch
    /// </summary>
    public const string SwitchBlue = "[--switch-bg-ring:var(--color-blue-700)]/90 [--switch-bg:var(--color-blue-600)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-blue-700)]/90 [--switch-shadow:var(--color-blue-900)]/20";

    /// <summary>
    /// Red switch
    /// </summary>
    public const string SwitchRed = "[--switch-bg-ring:var(--color-red-700)]/90 [--switch-bg:var(--color-red-600)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-red-700)]/90 [--switch-shadow:var(--color-red-900)]/20";

    /// <summary>
    /// Green switch
    /// </summary>
    public const string SwitchGreen = "[--switch-bg-ring:var(--color-green-700)]/90 [--switch-bg:var(--color-green-600)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-green-700)]/90 [--switch-shadow:var(--color-green-900)]/20";

    /// <summary>
    /// Orange switch
    /// </summary>
    public const string SwitchOrange = "[--switch-bg-ring:var(--color-orange-600)]/90 [--switch-bg:var(--color-orange-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-orange-600)]/90 [--switch-shadow:var(--color-orange-900)]/20";

    /// <summary>
    /// Amber switch
    /// </summary>
    public const string SwitchAmber = "[--switch-bg-ring:var(--color-amber-500)]/80 [--switch-bg:var(--color-amber-400)] dark:[--switch-bg-ring:transparent] [--switch-ring:transparent] [--switch-shadow:transparent] [--switch:var(--color-amber-950)]";

    /// <summary>
    /// Yellow switch
    /// </summary>
    public const string SwitchYellow = "[--switch-bg-ring:var(--color-yellow-400)]/80 [--switch-bg:var(--color-yellow-300)] dark:[--switch-bg-ring:transparent] [--switch-ring:transparent] [--switch-shadow:transparent] [--switch:var(--color-yellow-950)]";

    /// <summary>
    /// Lime switch
    /// </summary>
    public const string SwitchLime = "[--switch-bg-ring:var(--color-lime-400)]/80 [--switch-bg:var(--color-lime-300)] dark:[--switch-bg-ring:transparent] [--switch-ring:transparent] [--switch-shadow:transparent] [--switch:var(--color-lime-950)]";

    /// <summary>
    /// Emerald switch
    /// </summary>
    public const string SwitchEmerald = "[--switch-bg-ring:var(--color-emerald-600)]/90 [--switch-bg:var(--color-emerald-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-emerald-600)]/90 [--switch-shadow:var(--color-emerald-900)]/20";

    /// <summary>
    /// Teal switch
    /// </summary>
    public const string SwitchTeal = "[--switch-bg-ring:var(--color-teal-700)]/90 [--switch-bg:var(--color-teal-600)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-teal-700)]/90 [--switch-shadow:var(--color-teal-900)]/20";

    /// <summary>
    /// Cyan switch
    /// </summary>
    public const string SwitchCyan = "[--switch-bg-ring:var(--color-cyan-400)]/80 [--switch-bg:var(--color-cyan-300)] dark:[--switch-bg-ring:transparent] [--switch-ring:transparent] [--switch-shadow:transparent] [--switch:var(--color-cyan-950)]";

    /// <summary>
    /// Sky switch
    /// </summary>
    public const string SwitchSky = "[--switch-bg-ring:var(--color-sky-600)]/80 [--switch-bg:var(--color-sky-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-sky-600)]/80 [--switch-shadow:var(--color-sky-900)]/20";

    /// <summary>
    /// Indigo switch
    /// </summary>
    public const string SwitchIndigo = "[--switch-bg-ring:var(--color-indigo-600)]/90 [--switch-bg:var(--color-indigo-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-indigo-600)]/90 [--switch-shadow:var(--color-indigo-900)]/20";

    /// <summary>
    /// Violet switch
    /// </summary>
    public const string SwitchViolet = "[--switch-bg-ring:var(--color-violet-600)]/90 [--switch-bg:var(--color-violet-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-violet-600)]/90 [--switch-shadow:var(--color-violet-900)]/20";

    /// <summary>
    /// Purple switch
    /// </summary>
    public const string SwitchPurple = "[--switch-bg-ring:var(--color-purple-600)]/90 [--switch-bg:var(--color-purple-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-purple-600)]/90 [--switch-shadow:var(--color-purple-900)]/20";

    /// <summary>
    /// Fuchsia switch
    /// </summary>
    public const string SwitchFuchsia = "[--switch-bg-ring:var(--color-fuchsia-600)]/90 [--switch-bg:var(--color-fuchsia-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-fuchsia-600)]/90 [--switch-shadow:var(--color-fuchsia-900)]/20";

    /// <summary>
    /// Pink switch
    /// </summary>
    public const string SwitchPink = "[--switch-bg-ring:var(--color-pink-600)]/90 [--switch-bg:var(--color-pink-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-pink-600)]/90 [--switch-shadow:var(--color-pink-900)]/20";

    /// <summary>
    /// Rose switch
    /// </summary>
    public const string SwitchRose = "[--switch-bg-ring:var(--color-rose-600)]/90 [--switch-bg:var(--color-rose-500)] dark:[--switch-bg-ring:transparent] [--switch:white] [--switch-ring:var(--color-rose-600)]/90 [--switch-shadow:var(--color-rose-900)]/20";
}
