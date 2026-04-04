import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { Button } from '../Button';

describe('Button', () => {
  it('should render button with children', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });

  it('should apply primary variant by default', () => {
    render(<Button>Primary Button</Button>);
    const button = screen.getByText('Primary Button');
    expect(button).toHaveClass('bg-gradient-to-r', 'from-blue-600', 'to-purple-600');
  });

  it('should apply secondary variant', () => {
    render(<Button variant='secondary'>Secondary Button</Button>);
    const button = screen.getByText('Secondary Button');
    expect(button).toHaveClass('bg-white/10', 'backdrop-blur-sm');
  });

  it('should apply outline variant', () => {
    render(<Button variant='outline'>Outline Button</Button>);
    const button = screen.getByText('Outline Button');
    expect(button).toHaveClass('border-2', 'border-current');
  });

  it('should apply ghost variant', () => {
    render(<Button variant='ghost'>Ghost Button</Button>);
    const button = screen.getByText('Ghost Button');
    expect(button).toHaveClass('hover:bg-white/10');
  });

  it('should apply danger variant', () => {
    render(<Button variant='danger'>Danger Button</Button>);
    const button = screen.getByText('Danger Button');
    expect(button).toHaveClass('bg-red-600', 'text-white');
  });

  it('should apply medium size by default', () => {
    render(<Button>Medium Button</Button>);
    const button = screen.getByText('Medium Button');
    expect(button).toHaveClass('px-6', 'py-3', 'text-base');
  });

  it('should apply small size', () => {
    render(<Button size='sm'>Small Button</Button>);
    const button = screen.getByText('Small Button');
    expect(button).toHaveClass('px-4', 'py-2', 'text-sm');
  });

  it('should apply large size', () => {
    render(<Button size='lg'>Large Button</Button>);
    const button = screen.getByText('Large Button');
    expect(button).toHaveClass('px-8', 'py-4', 'text-lg');
  });

  it('should show loading state', () => {
    render(<Button isLoading>Loading Button</Button>);
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    expect(screen.queryByText('Loading Button')).not.toBeInTheDocument();
  });

  it('should disable button when isLoading is true', () => {
    render(<Button isLoading>Button</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('should disable button when disabled prop is true', () => {
    render(<Button disabled>Disabled Button</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  it('should apply fullWidth class', () => {
    render(<Button fullWidth>Full Width Button</Button>);
    const button = screen.getByText('Full Width Button');
    expect(button).toHaveClass('w-full');
  });

  it('should handle click events', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Clickable Button</Button>);
    const button = screen.getByText('Clickable Button');
    fireEvent.click(button);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('should not trigger click when disabled', () => {
    const handleClick = jest.fn();
    render(
      <Button onClick={handleClick} disabled>
        Disabled Button
      </Button>,
    );
    const button = screen.getByText('Disabled Button');
    fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('should not trigger click when loading', () => {
    const handleClick = jest.fn();
    render(
      <Button onClick={handleClick} isLoading>
        Loading Button
      </Button>,
    );
    const button = screen.getByRole('button');
    fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('should merge custom className', () => {
    render(<Button className='custom-class'>Custom Button</Button>);
    const button = screen.getByText('Custom Button');
    expect(button).toHaveClass('custom-class');
  });

  it('should pass through HTML button attributes', () => {
    render(
      <Button type='submit' name='submit-button'>
        Submit
      </Button>,
    );
    const button = screen.getByText('Submit');
    expect(button).toHaveAttribute('type', 'submit');
    expect(button).toHaveAttribute('name', 'submit-button');
  });

  it('should render loading spinner when isLoading', () => {
    render(<Button isLoading>Button</Button>);
    const svg = screen.getByRole('button').querySelector('svg');
    expect(svg).toBeInTheDocument();
    expect(svg).toHaveClass('animate-spin');
  });
});
