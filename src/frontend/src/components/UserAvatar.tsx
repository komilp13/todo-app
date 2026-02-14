'use client';

/**
 * UserAvatar Component
 * Displays user initials in a colored circle
 */
interface UserAvatarProps {
  displayName: string;
  size?: 'sm' | 'md' | 'lg';
}

const AVATAR_COLORS = [
  'bg-red-500',
  'bg-blue-500',
  'bg-green-500',
  'bg-purple-500',
  'bg-yellow-500',
  'bg-pink-500',
  'bg-indigo-500',
  'bg-cyan-500',
];

export default function UserAvatar({ displayName, size = 'md' }: UserAvatarProps) {
  // Get initials from display name
  const initials = displayName
    .split(' ')
    .map((word) => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  // Select color based on name hash
  const colorIndex = displayName.charCodeAt(0) % AVATAR_COLORS.length;
  const bgColor = AVATAR_COLORS[colorIndex];

  // Size classes
  const sizeClasses = {
    sm: 'w-6 h-6 text-xs',
    md: 'w-8 h-8 text-sm',
    lg: 'w-10 h-10 text-base',
  };

  return (
    <div
      className={`${sizeClasses[size]} ${bgColor} rounded-full flex items-center justify-center text-white font-semibold`}
      title={displayName}
    >
      {initials}
    </div>
  );
}
