/**
 * Simple Header component for testing
 */
export default function Header({ title }: { title: string }) {
  return (
    <header className="bg-blue-600 text-white p-4">
      <h1 className="text-2xl font-bold">{title}</h1>
    </header>
  );
}
