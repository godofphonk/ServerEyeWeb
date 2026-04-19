import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '../Card';

describe('Card', () => {
  it('should render children', () => {
    render(<Card>Card content</Card>);
    expect(screen.getByText('Card content')).toBeInTheDocument();
  });

  it('should apply default styles', () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.querySelector('.bg-gray-900\\/50');
    expect(card).toHaveClass('backdrop-blur-xl', 'rounded-2xl');
  });

  it('should not apply hover styles by default', () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.querySelector('.bg-gray-900\\/50');
    expect(card).not.toHaveClass('hover:border-white/20');
  });

  it('should apply hover styles when hover prop is true', () => {
    const { container } = render(<Card hover>Content</Card>);
    const card = container.querySelector('.bg-gray-900\\/50');
    expect(card).toHaveClass('hover:border-white/20');
  });

  it('should merge custom className', () => {
    const { container } = render(<Card className='custom-class'>Content</Card>);
    const card = container.querySelector('.bg-gray-900\\/50');
    expect(card).toHaveClass('custom-class');
  });
});

describe('CardHeader', () => {
  it('should render children', () => {
    render(<CardHeader>Header content</CardHeader>);
    expect(screen.getByText('Header content')).toBeInTheDocument();
  });

  it('should apply default margin styles', () => {
    render(<CardHeader>Header</CardHeader>);
    const header = screen.getByText('Header').closest('div');
    expect(header).toHaveClass('mb-4');
  });

  it('should merge custom className', () => {
    render(<CardHeader className='custom-header'>Header</CardHeader>);
    const header = screen.getByText('Header').closest('div');
    expect(header).toHaveClass('custom-header');
  });
});

describe('CardTitle', () => {
  it('should render children inside h3', () => {
    render(<CardTitle>My Title</CardTitle>);
    const title = screen.getByText('My Title');
    expect(title.tagName).toBe('H3');
  });

  it('should apply heading styles', () => {
    render(<CardTitle>Title</CardTitle>);
    const title = screen.getByText('Title');
    expect(title).toHaveClass('text-2xl', 'font-bold');
  });

  it('should merge custom className', () => {
    render(<CardTitle className='custom-title'>Title</CardTitle>);
    const title = screen.getByText('Title');
    expect(title).toHaveClass('custom-title');
  });
});

describe('CardDescription', () => {
  it('should render children inside a paragraph', () => {
    render(<CardDescription>Description text</CardDescription>);
    const desc = screen.getByText('Description text');
    expect(desc.tagName).toBe('P');
  });

  it('should apply muted text styles', () => {
    render(<CardDescription>Description</CardDescription>);
    const desc = screen.getByText('Description');
    expect(desc).toHaveClass('text-gray-400');
  });

  it('should merge custom className', () => {
    render(<CardDescription className='custom-desc'>Description</CardDescription>);
    const desc = screen.getByText('Description');
    expect(desc).toHaveClass('custom-desc');
  });
});

describe('CardContent', () => {
  it('should render children', () => {
    render(<CardContent>Content section</CardContent>);
    expect(screen.getByText('Content section')).toBeInTheDocument();
  });

  it('should merge custom className', () => {
    render(<CardContent className='custom-content'>Content</CardContent>);
    const content = screen.getByText('Content').closest('div');
    expect(content).toHaveClass('custom-content');
  });
});

describe('CardFooter', () => {
  it('should render children', () => {
    render(<CardFooter>Footer content</CardFooter>);
    expect(screen.getByText('Footer content')).toBeInTheDocument();
  });

  it('should apply border-top styles', () => {
    render(<CardFooter>Footer</CardFooter>);
    const footer = screen.getByText('Footer').closest('div');
    expect(footer).toHaveClass('border-t');
  });

  it('should merge custom className', () => {
    render(<CardFooter className='custom-footer'>Footer</CardFooter>);
    const footer = screen.getByText('Footer').closest('div');
    expect(footer).toHaveClass('custom-footer');
  });
});
