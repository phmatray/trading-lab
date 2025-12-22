namespace TradingStrat.Web.Utilities;

/// <summary>
/// Centralized Tailwind CSS utility classes for consistent styling across the application.
/// Eliminates ~6KB of duplicated CSS class strings.
/// </summary>
public static class TailwindStyles
{
    // ========== Form Inputs ==========

    /// <summary>
    /// Standard text input field styling with dark mode support.
    /// </summary>
    public const string InputText = "w-full rounded-lg border-gray-300 dark:border-dark-border bg-white dark:bg-dark-elevated text-gray-900 dark:text-dark-text-primary focus:border-trading-blue dark:focus:border-dark-accent-blue focus:ring-trading-blue dark:focus:ring-dark-accent-blue";

    /// <summary>
    /// Standard select/dropdown styling with dark mode support.
    /// </summary>
    public const string InputSelect = "w-full rounded-lg border-gray-300 dark:border-dark-border bg-white dark:bg-dark-elevated text-gray-900 dark:text-dark-text-primary focus:border-trading-blue dark:focus:border-dark-accent-blue focus:ring-trading-blue dark:focus:ring-dark-accent-blue";

    /// <summary>
    /// Standard date input field styling with dark mode support.
    /// </summary>
    public const string InputDate = "w-full rounded-lg border-gray-300 dark:border-dark-border bg-white dark:bg-dark-elevated text-gray-900 dark:text-dark-text-primary focus:border-trading-blue dark:focus:border-dark-accent-blue focus:ring-trading-blue dark:focus:ring-dark-accent-blue";

    /// <summary>
    /// Standard number input field styling with dark mode support.
    /// </summary>
    public const string InputNumber = "w-full rounded-lg border-gray-300 dark:border-dark-border bg-white dark:bg-dark-elevated text-gray-900 dark:text-dark-text-primary focus:border-trading-blue dark:focus:border-dark-accent-blue focus:ring-trading-blue dark:focus:ring-dark-accent-blue";

    /// <summary>
    /// Standard textarea styling with dark mode support.
    /// </summary>
    public const string InputTextarea = "w-full rounded-lg border-gray-300 dark:border-dark-border bg-white dark:bg-dark-elevated text-gray-900 dark:text-dark-text-primary focus:border-trading-blue dark:focus:border-dark-accent-blue focus:ring-trading-blue dark:focus:ring-dark-accent-blue";

    // ========== Form Labels ==========

    /// <summary>
    /// Standard form label styling with dark mode support.
    /// </summary>
    public const string Label = "block text-sm font-medium text-gray-700 dark:text-dark-text-primary mb-1";

    /// <summary>
    /// Optional label suffix styling (e.g., "(optional)" text).
    /// </summary>
    public const string LabelOptional = "text-gray-500 dark:text-dark-text-secondary font-normal";

    // ========== Validation ==========

    /// <summary>
    /// Validation error message styling.
    /// </summary>
    public const string ValidationError = "text-sm text-red-600 dark:text-dark-danger mt-1";

    /// <summary>
    /// Validation success message styling.
    /// </summary>
    public const string ValidationSuccess = "text-sm text-green-600 dark:text-dark-success mt-1";

    // ========== Buttons ==========

    /// <summary>
    /// Primary button styling (blue background).
    /// </summary>
    public const string ButtonPrimary = "px-4 py-2 bg-trading-blue hover:bg-blue-700 dark:bg-dark-accent-blue dark:hover:bg-blue-600 text-white rounded-lg font-medium transition-colors";

    /// <summary>
    /// Secondary button styling (gray background).
    /// </summary>
    public const string ButtonSecondary = "px-4 py-2 bg-gray-200 hover:bg-gray-300 dark:bg-dark-elevated dark:hover:bg-gray-700 text-gray-800 dark:text-dark-text-primary rounded-lg font-medium transition-colors";

    /// <summary>
    /// Danger button styling (red background, for destructive actions).
    /// </summary>
    public const string ButtonDanger = "px-4 py-2 bg-red-600 hover:bg-red-700 dark:bg-dark-danger dark:hover:bg-red-700 text-white rounded-lg font-medium transition-colors";

    /// <summary>
    /// Success button styling (green background).
    /// </summary>
    public const string ButtonSuccess = "px-4 py-2 bg-green-600 hover:bg-green-700 dark:bg-dark-success dark:hover:bg-green-700 text-white rounded-lg font-medium transition-colors";

    /// <summary>
    /// Icon-only button styling (square, centered icon).
    /// </summary>
    public const string ButtonIcon = "p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-dark-elevated transition-colors";

    // ========== Cards ==========

    /// <summary>
    /// Standard card container styling with dark mode support.
    /// </summary>
    public const string Card = "bg-white dark:bg-dark-elevated rounded-lg shadow-sm p-6 border border-gray-200 dark:border-dark-border";

    /// <summary>
    /// Card header styling.
    /// </summary>
    public const string CardHeader = "text-lg font-semibold text-gray-900 dark:text-dark-text-primary mb-4";

    /// <summary>
    /// Card section divider.
    /// </summary>
    public const string CardDivider = "border-t border-gray-200 dark:border-dark-border my-4";

    // ========== Badges ==========

    /// <summary>
    /// Success badge (green).
    /// </summary>
    public const string BadgeSuccess = "px-2 py-1 text-xs font-medium rounded-full bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-400";

    /// <summary>
    /// Warning badge (yellow).
    /// </summary>
    public const string BadgeWarning = "px-2 py-1 text-xs font-medium rounded-full bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-400";

    /// <summary>
    /// Danger badge (red).
    /// </summary>
    public const string BadgeDanger = "px-2 py-1 text-xs font-medium rounded-full bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-400";

    /// <summary>
    /// Info badge (blue).
    /// </summary>
    public const string BadgeInfo = "px-2 py-1 text-xs font-medium rounded-full bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-400";

    /// <summary>
    /// Neutral badge (gray).
    /// </summary>
    public const string BadgeNeutral = "px-2 py-1 text-xs font-medium rounded-full bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-300";

    // ========== Typography ==========

    /// <summary>
    /// Page title styling.
    /// </summary>
    public const string PageTitle = "text-3xl font-bold text-gray-900 dark:text-dark-text-primary";

    /// <summary>
    /// Section heading styling.
    /// </summary>
    public const string SectionHeading = "text-xl font-semibold text-gray-900 dark:text-dark-text-primary mb-4";

    /// <summary>
    /// Subsection heading styling.
    /// </summary>
    public const string SubsectionHeading = "text-lg font-medium text-gray-900 dark:text-dark-text-primary mb-2";

    /// <summary>
    /// Standard body text.
    /// </summary>
    public const string BodyText = "text-gray-700 dark:text-dark-text-secondary";

    /// <summary>
    /// Muted text (less prominent).
    /// </summary>
    public const string TextMuted = "text-gray-500 dark:text-dark-text-tertiary";

    // ========== Metrics ==========

    /// <summary>
    /// Positive metric value (gains, increases).
    /// </summary>
    public const string MetricPositive = "text-green-600 dark:text-dark-success font-semibold";

    /// <summary>
    /// Negative metric value (losses, decreases).
    /// </summary>
    public const string MetricNegative = "text-red-600 dark:text-dark-danger font-semibold";

    /// <summary>
    /// Neutral metric value.
    /// </summary>
    public const string MetricNeutral = "text-gray-600 dark:text-dark-text-secondary font-semibold";

    // ========== Alerts ==========

    /// <summary>
    /// Success alert container.
    /// </summary>
    public const string AlertSuccess = "p-4 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-800 dark:text-green-300";

    /// <summary>
    /// Warning alert container.
    /// </summary>
    public const string AlertWarning = "p-4 rounded-lg bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 text-yellow-800 dark:text-yellow-300";

    /// <summary>
    /// Danger alert container.
    /// </summary>
    public const string AlertDanger = "p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-300";

    /// <summary>
    /// Info alert container.
    /// </summary>
    public const string AlertInfo = "p-4 rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-800 dark:text-blue-300";

    // ========== Tables ==========

    /// <summary>
    /// Table container styling.
    /// </summary>
    public const string Table = "min-w-full divide-y divide-gray-200 dark:divide-dark-border";

    /// <summary>
    /// Table header cell styling.
    /// </summary>
    public const string TableHeader = "px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-dark-text-secondary uppercase tracking-wider";

    /// <summary>
    /// Table body cell styling.
    /// </summary>
    public const string TableCell = "px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-dark-text-primary";

    /// <summary>
    /// Table row styling with hover effect.
    /// </summary>
    public const string TableRow = "hover:bg-gray-50 dark:hover:bg-dark-elevated transition-colors";
}
